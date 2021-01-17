#!/bin/bash

INSTANCE_ID=$1
if [[ -z $INSTANCE_ID ]]; then
    echo "Please pass ec2 instance id as argument"
    exit 0
fi
INSTANCE_DETAILS=$(aws ec2 describe-instances --instance-id $INSTANCE_ID)
AVAILABILITY_ZONE=$(echo $INSTANCE_DETAILS | jq -r '.Reservations[0].Instances[0].Placement.AvailabilityZone')
VOLUME_DETAILS=$(aws ec2 create-volume --availability-zone $AVAILABILITY_ZONE --size 50 --volume-type gp2 --tag-specifications 'ResourceType=volume,Tags=[{Key=Name,Value=dev-k8-rebuild-folder-to-folder}]')
VOLUME_ID=$(echo $VOLUME_DETAILS | jq -r '.VolumeId')
aws ec2 attach-volume --volume-id $VOLUME_ID --instance-id $INSTANCE_ID --device /dev/sda2 # /dev/xvdb
