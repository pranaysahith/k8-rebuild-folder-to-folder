#!/bin/bash

# get source code
sudo yum install -y git
git clone https://github.com/pranaysahith/k8-rebuild-folder-to-folder.git && cd k8-rebuild-folder-to-folder # TODO: update repo org name.
git checkout cicd_workflow # TODO: remove this line.
git clone https://github.com/k8-proxy/k8-rebuild.git --recursive && cd k8-rebuild && git submodule foreach git pull origin main && cd ../

# install docker, docker-compose
sudo amazon-linux-extras install docker -y
sudo service docker start
sudo usermod -a -G docker ec2-user
sudo chkconfig docker on
sudo curl -L "https://github.com/docker/compose/releases/download/1.27.4/docker-compose-$(uname -s)-$(uname -m)" -o /usr/bin/docker-compose
sudo chmod +x /usr/bin/docker-compose

# start applications
sudo docker-compose up -d
