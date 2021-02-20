"""Ref. https://www.tensorflow.org/tutorials/text/image_captioning"""

import os
import json
import pickle
import random
import collections
import numpy as np
import tensorflow as tf

def download_data():
    # Download caption annotation files
    data_path = '.\\data\\train2014'
    if not os.path.exists(os.path.abspath('.') + data_path + '\\annotations\\'):
        annotation_zip = tf.keras.utils.get_file('captions.zip',
                                                 cache_subdir=os.path.abspath('.') + data_path,
                                                 origin = 'http://images.cocodataset.org/annotations/annotations_trainval2014.zip',
                                                 extract = True)
        os.remove(annotation_zip)

    # Download image files
    if not os.path.exists(os.path.abspath('.') + data_path + '\\images\\'):
        image_zip = tf.keras.utils.get_file('train2014.zip',
                                            cache_subdir=os.path.abspath('.') + data_path,
                                            origin = 'http://images.cocodataset.org/zips/train2014.zip',
                                            extract = True)
        os.remove(image_zip)

def get_encoder_training_data(dataset_size=6000):
    dirname = os.path.dirname(__file__)
    image_path = os.path.abspath('.') + '.\\data\\train2014\\images\\'
    caption_path = os.path.abspath('.') + '.\\data\\train2014\\annotations\\captions_train2014.json'

    with open(caption_path, 'r') as f:
        annotations = json.load(f)
    # Group all captions together having the same image ID.
    image_path_to_caption = collections.defaultdict(list)
    for val in annotations['annotations']:
        caption = f"<start> {val['caption']} <end>"
        ipath = image_path + 'COCO_train2014_' + '%012d.jpg' % (val['image_id'])
        image_path_to_caption[ipath].append(caption)

    image_paths = list(image_path_to_caption.keys())
    random.shuffle(image_paths)

    # Select the first 6000 image_paths from the shuffled set.
    # Approximately each image id has 5 captions associated with it, so that will 
    # lead to 30,000 examples.
    train_image_paths = image_paths[:dataset_size]

    train_captions = []
    img_name_vector = []

    for image_path in train_image_paths:
        caption_list = image_path_to_caption[image_path]
        train_captions.extend(caption_list)
        img_name_vector.extend([image_path] * len(caption_list))
    
    return train_captions, img_name_vector

def load_image(image_path):
    img = tf.io.read_file(image_path)
    img = tf.image.decode_jpeg(img, channels=3)
    img = tf.image.resize(img, (299, 299))
    img = tf.keras.applications.inception_v3.preprocess_input(img)
    return img, image_path

def preprocess_tokenize_captions():
    train_captions, img_name_vector = get_encoder_training_data()

    # Choose the top 5000 words from the vocabulary
    tokenizer = tf.keras.preprocessing.text.Tokenizer(num_words=5000, oov_token="<unk>", filters=r'!"#$%&()*+.,-/:;=?@[\]^_`{|}~ ')
    tokenizer.fit_on_texts(train_captions)
    tokenizer.word_index['<pad>'] = 0
    tokenizer.index_word[0] = '<pad>'

    # Create the tokenized vectors
    train_seqs = tokenizer.texts_to_sequences(train_captions)

    # Pad each vector to the max_length of the captions
    # If you do not provide a max_length value, pad_sequences calculates it automatically
    cap_vector = tf.keras.preprocessing.sequence.pad_sequences(train_seqs, padding='post')

    # Calculates the max_length, which is used to store the attention weights
    max_length = max(len(t) for t in train_seqs)

    return tokenizer, cap_vector, max_length, img_name_vector

def train_test_split_data():
    tokenizer, cap_vector, max_length, img_name_vector = preprocess_tokenize_captions()

    img_to_cap_vector = collections.defaultdict(list)
    for img, cap in zip(img_name_vector, cap_vector):
        img_to_cap_vector[img].append(cap)

    # Create training and validation sets using an 80-20 split randomly.
    img_keys = list(img_to_cap_vector.keys())
    # random.shuffle(img_keys)

    slice_index = int(len(img_keys)*0.8)
    img_name_train_keys, img_name_val_keys = img_keys[:slice_index], img_keys[slice_index:]

    img_name_train = []
    cap_train = []
    for imgt in img_name_train_keys:
        capt_len = len(img_to_cap_vector[imgt])
        img_name_train.extend([imgt] * capt_len)
        cap_train.extend(img_to_cap_vector[imgt])

    img_name_val = []
    cap_val = []
    for imgv in img_name_val_keys:
        capv_len = len(img_to_cap_vector[imgv])
        img_name_val.extend([imgv] * capv_len)
        cap_val.extend(img_to_cap_vector[imgv])

    return img_name_train, cap_train, img_name_val, cap_val, tokenizer, max_length

def get_dataset(dateset_type='train'):
    BATCH_SIZE = 64
    BUFFER_SIZE = 1000
    # Load the numpy files
    def map_func(img_name, cap):
        img_tensor = np.load(img_name.decode('utf-8')+'.npy')
        return img_tensor, cap

    img_name_train, cap_train, img_name_val, cap_val, tokenizer, max_length = train_test_split_data()
    img_name_set, cap_set = [], []
    if dateset_type == 'evaluate':
        img_name_set, cap_set = img_name_val, cap_val
    else:
        img_name_set, cap_set = img_name_train, cap_train
    
    dataset = tf.data.Dataset.from_tensor_slices((img_name_set, cap_set))

    # Use map to load the numpy files in parallel
    dataset = dataset.map(lambda item1, item2: tf.numpy_function(
            map_func, [item1, item2], [tf.float32, tf.int32]),
            num_parallel_calls=tf.data.AUTOTUNE)

    # Shuffle and batch
    dataset = dataset.shuffle(BUFFER_SIZE).batch(BATCH_SIZE)
    dataset = dataset.prefetch(buffer_size=tf.data.AUTOTUNE)

    return dataset, BATCH_SIZE, tokenizer, max_length
