#!/bin/bash

INSTANCE_ID=$1
if [[ -z $INSTANCE_ID ]]; then
    echo "Please pass ec2 instance id as argument"
    exit 0
fi
VOLUME_SIZE=${2:-50} # Defaults to 50Gi
INSTANCE_DETAILS=$(aws ec2 describe-instances --instance-id $INSTANCE_ID)
AVAILABILITY_ZONE=$(echo $INSTANCE_DETAILS | jq -r '.Reservations[0].Instances[0].Placement.AvailabilityZone')
VOLUME_DETAILS=$(aws ec2 create-volume --availability-zone $AVAILABILITY_ZONE --size $VOLUME_SIZE --volume-type gp2 --tag-specifications 'ResourceType=volume,Tags=[{Key=Name,Value=dev-k8-rebuild-folder-to-folder}]')
VOLUME_ID=$(echo $VOLUME_DETAILS | jq -r '.VolumeId')
VOLUME_STATE=$(aws ec2 describe-volumes --volume-id $VOLUME_ID | jq -r '.Volumes[0].State')
until [ "$VOLUME_STATE" = "available" ]; do
    sleep 5s
    VOLUME_STATE=$(aws ec2 describe-volumes --volume-id $VOLUME_ID | jq -r '.Volumes[0].State')
    echo "Volume state is $VOLUME_STATE"
done
aws ec2 attach-volume --volume-id $VOLUME_ID --instance-id $INSTANCE_ID --device /dev/xvdb
