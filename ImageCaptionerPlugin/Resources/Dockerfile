FROM tensorflow/tensorflow:latest
RUN apt-get update && apt-get upgrade -y
RUN python -m pip install --upgrade pip
RUN pip install flask-restful

RUN mkdir /tmp/icmserver
WORKDIR /tmp/icmserver
COPY Model /tmp/icmserver

EXPOSE 5000
CMD python icm_server.py
