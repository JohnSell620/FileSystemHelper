"""Ref. https://www.tensorflow.org/tutorials/text/image_captioning"""

import gc
import os
import time
import utils
import numpy as np
from PIL import Image
from tqdm import tqdm
import tensorflow as tf
import matplotlib.pyplot as plt

dirname = os.path.dirname(__file__)

class BahdanauAttention(tf.keras.Model):
    def __init__(self):
        super(BahdanauAttention, self).__init__()
        self.W1 = tf.keras.layers.Dense(512)
        self.W2 = tf.keras.layers.Dense(512)
        self.V = tf.keras.layers.Dense(1)

    def call(self, x):
        # features(CNN_encoder output) shape == (batch_size, 64, embedding_dim)
        # hidden shape == (batch_size, hidden_size)
        (features, hidden) = x

        # hidden_with_time_axis shape == (batch_size, 1, hidden_size)
        hidden_with_time_axis = tf.expand_dims(hidden, 1)

        # attention_hidden_layer shape == (batch_size, 64, units)
        attention_hidden_layer = (tf.nn.tanh(self.W1(features) +
                                            self.W2(hidden_with_time_axis)))

        # score shape == (batch_size, 64, 1)
        # This gives you an unnormalized score for each image feature.
        score = self.V(attention_hidden_layer)

        # attention_weights shape == (batch_size, 64, 1)
        attention_weights = tf.nn.softmax(score, axis=1)

        # context_vector shape after sum == (batch_size, hidden_size)
        context_vector = attention_weights * features
        context_vector = tf.reduce_sum(context_vector, axis=1)

        return context_vector, attention_weights


class CNN_Encoder(tf.keras.Model):
    # Since you have already extracted the features and dumped it using pickle
    # This encoder passes those features through a Fully connected layer
    def __init__(self):
        super(CNN_Encoder, self).__init__()
        # shape after fc == (batch_size, 64, embedding_dim=256)
        self.fc = tf.keras.layers.Dense(256)

    @tf.function #(input_signature = [tf.TensorSpec(shape=[1, 64, features_shape],)])
    def call(self, x):
        x = self.fc(x)
        x = tf.nn.relu(x)
        return x


class RNN_Decoder(tf.keras.Model):
    def __init__(self):
        super(RNN_Decoder, self).__init__()
        self.embedding = tf.keras.layers.Embedding(5000, 256)
        self.gru = tf.keras.layers.GRU(512,
                                       return_sequences=True,
                                       return_state=True,
                                       recurrent_initializer='glorot_uniform')
        self.fc1 = tf.keras.layers.Dense(512)
        self.fc2 = tf.keras.layers.Dense(5000)
        self.attention = BahdanauAttention()

    # @tf.function(input_signature=[(tf.TensorSpec(shape=[None, 1], dtype=tf.int32, name='x'),
    #                               tf.TensorSpec(shape=[None, 64, 256], dtype=tf.float32, name='features'),
    @tf.function
    def call(self, x):
        (x, features, hidden) = x
        # defining attention as a separate model
        context_vector, attention_weights = self.attention((features, hidden))

        # x shape after passing through embedding == (batch_size, 1, embedding_dim)
        x = self.embedding(x)

        # x shape after concatenation == (batch_size, 1, embedding_dim + hidden_size)
        x = tf.concat([tf.expand_dims(context_vector, 1), x], axis=-1)
        # x = tf.concat([tf.expand_dims(context_vector, 1), x], axis=-1)

        # passing the concatenated vector to the GRU
        output, state = self.gru(x)

        # shape == (batch_size, max_length, hidden_size)
        x = self.fc1(output)

        # x shape == (batch_size * max_length, hidden_size)
        x = tf.reshape(x, (-1, x.shape[2]))

        # output shape == (batch_size * max_length, vocab)
        x = self.fc2(x)

        return x, state, attention_weights

    def reset_state(self, batch_size):
        return tf.zeros((batch_size, 512))


class ImageCaptioner(tf.keras.Model):
    """
        embedding_dim = 256
        units = 512
        vocab_size = 5000
        attention_features_shape = 64
		features_shape = 2048
    """
    def __init__(self):
        super(ImageCaptioner, self).__init__()

        image_model = tf.keras.applications.InceptionV3(include_top=False, weights='imagenet')
        new_input = image_model.input
        hidden_layer = image_model.layers[-1].output
        self.image_features_extract_model = tf.keras.Model(new_input, hidden_layer)

        self.encoder = CNN_Encoder()
        self.decoder = RNN_Decoder()
        self.attention_features_shape = 64

        self.optimizer = tf.keras.optimizers.Adam()
        self.loss_object = tf.keras.losses.SparseCategoricalCrossentropy(from_logits=True, reduction='none')
        self.checkpoint_path = os.path.join(dirname, '.\\data\\checkpoints\\train\\')
        self.tokenizer, _, self.max_length, _ = utils.preprocess_tokenize_captions()
    
    def call(self, x):
        """ input_shape=(1, 299, 299, 3) """
        hidden = tf.random.uniform([1, 512], minval=0.1, maxval=1.0, dtype=tf.dtypes.float32) # self.decoder.reset_state(batch_size=1)
        x = self.image_features_extract_model(x)
        x = tf.reshape(x, (x.shape[0], -1, x.shape[3]))
        x = self.encoder(x)
        dec_input = tf.expand_dims([self.tokenizer.word_index['<start>']], 0)
        result = []
        for i in range(self.max_length):
            predictions, hidden, attention_weights = self.decoder((dec_input, x, hidden))
            predicted_id = tf.random.categorical(predictions, 1)[0][0].numpy()
            result.append(self.tokenizer.index_word[predicted_id])
            if self.tokenizer.index_word[predicted_id] == '<end>':
                return result
            dec_input = tf.expand_dims([predicted_id], 0)
        return result

    def train_cache_encoder_features(self, dataset_size=10000):
        """Initialize encoder before training since this could become a bottleneck"""
        _, img_name_vector = utils.get_encoder_training_data(dataset_size=dataset_size)
        # Get unique images
        encode_train = sorted(set(img_name_vector))
        # del img_name_vector
        # gc.collect()

        # Feel free to change batch_size according to your system configuration
        image_dataset = tf.data.Dataset.from_tensor_slices(encode_train)
        image_dataset = image_dataset.map(utils.load_image, num_parallel_calls=tf.data.AUTOTUNE).batch(32)

        for img, path in tqdm(image_dataset):
            batch_features = self.image_features_extract_model(img)
            batch_features = tf.reshape(batch_features, (batch_features.shape[0], -1, batch_features.shape[3]))

            for bf, p in zip(batch_features, path):
                path_of_feature = p.numpy().decode("utf-8")
                np.save(path_of_feature, bf.numpy())

    def loss_function(self, real, pred):
        mask = tf.math.logical_not(tf.math.equal(real, 0))
        loss_ = self.loss_object(real, pred)
        mask = tf.cast(mask, dtype=loss_.dtype)
        loss_ *= mask
        return tf.reduce_mean(loss_)

    @tf.function
    def train_step(self, img_tensor, target):
        # Initializing the hidden state for each batch
        # because the captions are not related from image to image
        hidden = self.decoder.reset_state(batch_size=target.shape[0])
        dec_input = tf.expand_dims([self.tokenizer.word_index['<start>']] * target.shape[0], 1)

        loss = 0
        with tf.GradientTape() as tape:
            features = self.encoder(img_tensor)
            for i in range(1, target.shape[1]):
                # passing the features through the decoder
                predictions, hidden, _ = self.decoder((dec_input, features, hidden))
                loss += self.loss_function(target[:, i], predictions)
                # using teacher forcing
                dec_input = tf.expand_dims(target[:, i], 1)

        total_loss = (loss / int(target.shape[1]))
        trainable_variables = self.encoder.trainable_variables + self.decoder.trainable_variables
        gradients = tape.gradient(loss, trainable_variables)
        self.optimizer.apply_gradients(zip(gradients, trainable_variables))
        return loss, total_loss

    def train(self, epochs=50, plot_loss=False):
        ckpt = tf.train.Checkpoint(encoder=self.encoder,
                                   decoder=self.decoder,
                                   optimizer=self.optimizer)
        ckpt_manager = tf.train.CheckpointManager(ckpt, self.checkpoint_path, max_to_keep=5)
        
        start_epoch = 0
        if ckpt_manager.latest_checkpoint:
            start_epoch = int(ckpt_manager.latest_checkpoint.split('-')[-1])
            # restoring the latest checkpoint in checkpoint_path
            ckpt.restore(ckpt_manager.latest_checkpoint)

        loss_plot = []
        dataset, num_steps, self.tokenizer, self.max_length = utils.get_dataset(dateset_type='train')
        for epoch in range(start_epoch, epochs):
            start = time.time()
            total_loss = 0

            for (batch, (img_tensor, target)) in enumerate(dataset):
                batch_loss, t_loss = self.train_step(img_tensor, target)
                total_loss += t_loss

                if batch % 100 == 0:
                    print ('Epoch {} Batch {} Loss {:.4f}'.format(
                        epoch + 1, batch, batch_loss.numpy() / int(target.shape[1])))
            # storing the epoch end loss value to plot later
            loss_plot.append(total_loss / num_steps)

            if epoch % 5 == 0:
                ckpt_manager.save()

            print ('Epoch {} Loss {:.6f}'.format(epoch + 1, total_loss/num_steps))
            print ('Time taken for 1 epoch {} sec\n'.format(time.time() - start))
        
        if plot_loss:
            plt.plot(loss_plot)
            plt.xlabel('Epochs')
            plt.ylabel('Loss')
            plt.title('Loss Plot')
            plt.show()

    def evaluate(self, image):
        if self.tokenizer == None and self.max_length == None:
            print("Model is untrained.")
            return

        attention_plot = np.zeros((self.max_length, self.attention_features_shape))

        hidden = self.decoder.reset_state(batch_size=1)

        temp_input = tf.expand_dims(utils.load_image(image)[0], 0)
        img_tensor_val = self.image_features_extract_model(temp_input)
        img_tensor_val = tf.reshape(img_tensor_val, (img_tensor_val.shape[0], -1, img_tensor_val.shape[3]))

        features = self.encoder(img_tensor_val)

        dec_input = tf.expand_dims([self.tokenizer.word_index['<start>']], 0)
        result = []

        for i in range(self.max_length):
            predictions, hidden, attention_weights = self.decoder((dec_input, features, hidden))

            attention_plot[i] = tf.reshape(attention_weights, (-1, )).numpy()

            predicted_id = tf.random.categorical(predictions, 1)[0][0].numpy()
            result.append(self.tokenizer.index_word[predicted_id])

            if self.tokenizer.index_word[predicted_id] == '<end>':
                return result, attention_plot

            dec_input = tf.expand_dims([predicted_id], 0)

        attention_plot = attention_plot[:len(result), :]
        return result, attention_plot

    def plot_attention(self):
        _, _, img_name_val, cap_val, _, _ = utils.train_test_split_data()
        rid = np.random.randint(0, len(img_name_val))
        image = img_name_val[rid]
        real_caption = ' '.join([self.tokenizer.index_word[i] for i in cap_val[rid] if i not in [0]])

        result, attention_plot = self.evaluate(image)

        print ('Real Caption:', real_caption)
        print ('Prediction Caption:', ' '.join(result))
        temp_image = np.array(Image.open(image))

        fig = plt.figure(figsize=(10, 10))

        len_result = len(result)
        for l in range(len_result):
            temp_att = np.resize(attention_plot[l], (8, 8))
            ax = fig.add_subplot(len_result//2, len_result//2, l+1)
            ax.set_title(result[l])
            img = ax.imshow(temp_image)
            ax.imshow(temp_att, cmap='gray', alpha=0.6, extent=img.get_extent())

        plt.tight_layout()
        plt.show()

    def save_model(self):
        self.image_features_extract_model.save(os.path.join(dirname, '.\\data\\saved_tf\\image_features_extract_model'))
        self.encoder.save(os.path.join(dirname, '.\\data\\saved_tf\\encoder'))
        tf.saved_model.save(
            self.decoder,
            os.path.join(dirname, '.\\data\\saved_tf\\decoder'),
            signatures=self.decoder.call.get_concrete_function((
            tf.TensorSpec(shape=[None, 1], dtype=tf.int32, name='x'),
            tf.TensorSpec(shape=[None, 64, 256], dtype=tf.float32, name="features"),
            tf.TensorSpec(shape=[None, 512], dtype=tf.float32, name="hidden"))))
    
    def get_config(self):
        pass

    def load_model(self):
        image_feature_extract_model = tf.keras.models.load_model(
            filepath=os.path.join(dirname, '.\\data\\saved_tf\\image_features_extract_model'), compile=False)
        encoder = tf.keras.models.load_model(
            filepath=os.path.join(dirname, '.\\data\\saved_tf\\encoder'), compile=False)
        decoder = tf.keras.models.load_model(
            filepath=os.path.join(dirname, '.\\data\\saved_tf\\decoder'), compile=False)

    def summary(self):
        self.image_features_extract_model.summary()
        self.encoder.summary()
        self.decoder.summary()