import model
import utils
import tensorflow as tf

# One-time call
# utils.download_data()

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

imgcap_model = model.ImageCaptioner()
imgcap_model.train_cache_encoder_features(dataset_size=6000)
imgcap_model.train(epochs=10)
imgcap_model.plot_attention()
imgcap_model.save_model()
