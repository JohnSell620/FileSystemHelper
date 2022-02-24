FROM tensorflow/tensorflow:latest-gpu
RUN apt-get update && apt-get upgrade -y
RUN python -m pip install --upgrade pip
RUN pip install flask-restful tqdm

RUN mkdir /tmp/icmserver
WORKDIR /tmp/icmserver
COPY model /tmp/icmserver

EXPOSE 5000
