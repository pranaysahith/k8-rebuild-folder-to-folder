#!/bin/bash

# mount the attached volume to /data/folder-to-folder
MOUNT_PATH=${1:-/data/folder-to-folder}
sudo file -s /dev/xvdb
sudo mkfs -t xfs /dev/xvdb
sudo mkdir -p $MOUNT_PATH
sudo mount /dev/xvdb $MOUNT_PATH
sudo chown -R $USER:$USER $MOUNT_PATH
sudo cp /etc/fstab /etc/fstab.orig
block_id=$(sudo blkid | grep /dev/xvdb | cut -d" " -f2 | tr -d '"')
echo $block_id $MOUNT_PATH xfs defaults,nofail 0 2 | sudo tee -a /etc/fstab
sudo umount $MOUNT_PATH
sudo mount -a
cd $MOUNT_PATH && mkdir -p input && mkdir -p output && mkdir -p error && mkdir -p log
