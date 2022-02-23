from flask import Flask, jsonify, request
from flask_restful import Api
import model

app = Flask(__name__)

imgcap_model = model.ImageCaptioner()
imgcap_model.load_model()

@app.route('/', methods=['GET'])
def hello():
    return jsonify({'message': 'Image-captioner model server'})

@app.route('/caption/<image>', methods=['POST'])
def caption_image(image):
    file = request.files['image']
    caption = imgcap_model.caption(file.read())
    return jsonify({'caption': ' '.join(caption)})

if __name__ == '__main__':
    app.run(debug=True)