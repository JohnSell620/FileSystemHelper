from distutils.log import debug
import sys
import model
import tensorflow as tf

from flask import Flask, jsonify, request
from flask_restful import Api


def train_model(epochs=14, cache_features=False, dataset_size=6000):
    imgcap_model = model.ImageCaptioner()
    if cache_features:
        imgcap_model.train_cache_encoder_features(dataset_size=dataset_size)
    imgcap_model.train(epochs=epochs)
    imgcap_model.save_model()

def serve():
    imgcap_model = model.ImageCaptioner()
    imgcap_model.load_model()

    app = Flask(__name__)

    @app.route('/', methods=['GET'])
    def index():
        return jsonify({'message': '*** image-captioner model server ***'})

    @app.route('/caption/<image>', methods=['POST'])
    def caption_image(image):
        file = request.files['image']
        caption = imgcap_model.caption(file.read())
        return jsonify({'caption': ' '.join(caption)})

    app.run(debug=False)

if __name__ == '__main__':    
    args = sys.argv[1:]
    if args:
        gpus = tf.config.experimental.list_physical_devices('GPU')
        if gpus:
            try:
                # Currently, memory growth needs to be the same across GPUs
                for gpu in gpus:
                    tf.config.experimental.set_memory_growth(gpu, True)
                logical_gpus = tf.config.experimental.list_logical_devices('GPU')
                print(len(gpus), "Physical GPUs,", len(logical_gpus), "Logical GPUs")
            except RuntimeError as e:
                # Memory growth must be set before GPUs have been initialized
                print(e)

        if args[0] == '--train':
            if len(args) == 1:
                print("usage: python", __file__, "--train [no. epochs]")
            else:
                train_model(int(args[1]))
        elif args[0] == '--serve':
            serve()
        else:
            print("main.py: unrecognized command line argument.")
    else:
        print("main.py: no command line argument(s) given.")