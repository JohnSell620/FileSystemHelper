"""Ref. https://www.tensorflow.org/tutorials/text/image_captioning"""

import os
import json
import numpy as np
import tensorflow as tf


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
        
        def reset_state(self, batch_size):
            return tf.zeros((batch_size, 512))

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
        self.tokenizer, self.max_length = self.preprocess_tokenize_captions()

        self.cwd = os.getcwd()
        
    def load_image(self, imgbyte_array):
        img = tf.image.decode_jpeg(imgbyte_array, channels=3)
        img = tf.image.resize(img, (299, 299))
        img = tf.keras.applications.inception_v3.preprocess_input(img)
        return img

    def preprocess_tokenize_captions(self):
        with open(os.getcwd() + '/annotations/captions_train2014.json', 'r') as f:
            annotations = json.load(f)
        
        # Build vocabulary
        vocab = []
        for val in annotations['annotations']:
            vocab.append(f"<start> {val['caption']} <end>")

        # Choose the top 5000 words from the vocabulary
        tokenizer = tf.keras.preprocessing.text.Tokenizer(num_words=5000, oov_token="<unk>", filters=r'!"#$%&()*+.,-/:;=?@[\]^_`{|}~ ')
        tokenizer.fit_on_texts(vocab)
        tokenizer.word_index['<pad>'] = 0
        tokenizer.index_word[0] = '<pad>'

        # Create the tokenized vectors
        train_seqs = tokenizer.texts_to_sequences(vocab)

        # Calculates the max_length, which is used to store the attention weights
        max_length = max(len(t) for t in train_seqs)

        return tokenizer, max_length
    
    def call(self, x):
        """ input_shape=(1, 299, 299, 3) """
        hidden = tf.random.uniform([1, 512], minval=0.1, maxval=1.0, dtype=tf.dtypes.float32)
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

    def caption(self, imgbyte_arr):
        if self.tokenizer == None and self.max_length == None:
            print("Model is untrained.")
            return

        temp_input = tf.expand_dims(self.load_image(imgbyte_arr), 0)
        img_tensor_val = self.image_features_extract_model(temp_input)
        img_tensor_val = tf.reshape(img_tensor_val, (img_tensor_val.shape[0], -1, img_tensor_val.shape[3]))

        features = self.encoder(img_tensor_val)

        result = []
        hidden = tf.zeros((1, 512))
        dec_input = tf.expand_dims([self.tokenizer.word_index['<start>']], 0)

        for i in range(self.max_length):
            predictions, hidden, attention_weights = self.decoder((dec_input, features, hidden))

            if i == 0:
                pw_idx = tf.random.categorical(predictions, 1)

            predicted_id = tf.random.categorical(predictions, 1)[0][0].numpy()
            result.append(self.tokenizer.index_word[predicted_id])

            if self.tokenizer.index_word[predicted_id] == '<end>':
                return result

            dec_input = tf.expand_dims([predicted_id], 0)

        return ' '.join(' '.join(result))

    def load_model(self):
        self.image_features_extract_model = tf.keras.models.load_model(
            filepath=self.cwd + '/image_features_extract_model', compile=False)
        self.encoder = tf.keras.models.load_model(
            filepath=self.cwd + '/encoder', compile=False)
        self.decoder = tf.keras.models.load_model(
            filepath=self.cwd + '/decoder', compile=False)
