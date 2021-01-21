#!/bin/bash

OVA_PATH=$1 # example argument: s3://glasswall-sow-ova/vms/k8-rebuild-folder-to-folder/some.ova
if [[ -z "$OVA_PATH" ]]; then
  echo "Pleas pass s3 path of OVA as argument. Example: s3://glasswall-sow-ova/some.ova"
  exit -1
fi
BUCKET_NAME=$( echo $OVA_PATH | sed 's|s3://||' | cut -d"/" -f1 )
FILE_PATH=$( echo $OVA_PATH | sed 's|s3://||' | cut -d"/" -f 2- )
cat > packer/containers.json <<EOF
[
    {
        "Description": "filedrop OVA",
        "Format": "ova",
        "UserBucket": {
            "S3Bucket": "$BUCKET_NAME",
            "S3Key": "$FILE_PATH"
      }
    }
]
EOF
IMPORT_TASK=$(aws ec2 import-image --description "k8-rebuild-folder-to-folder" --disk-containers "file://packer/containers.json")
IMPORT_ID=$(echo $IMPORT_TASK | jq -r .ImportTaskId)
until [ "$RESPONSE" = "completed" ]
do
  RESPONSE=$(aws ec2 describe-import-image-tasks --import-task-ids $IMPORT_ID | jq -r '.ImportImageTasks[0].Status')
  StatusMessage=$(aws ec2 describe-import-image-tasks --import-task-ids $IMPORT_ID | jq -r '.ImportImageTasks[0].StatusMessage')
  if [[ "deleted" == "$RESPONSE" ]]; then
    echo "Failed to import OVA"
    echo "OVA Import status is $RESPONSE"
    echo "Status message is $StatusMessage"
    exit -1
  elif [[ "active" == "$RESPONSE" ]]; then
    echo "OVA Import status is $RESPONSE"
    echo "Status message is $StatusMessage"
    sleep 30
  fi
done
echo "OVA Import status is $RESPONSE"
AMI_ID=$(echo $IMPORT_TASK | jq -r '.ImportImageTasks[0].ImageId')
echo "Imported AMI ID is: ${AMI_ID}"
