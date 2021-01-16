#!/bin/bash
EXPORT_ID=$(cat out.json | jq -r .[].ExportTaskId)
echo $EXPORT_ID
# increase file size until 1KB
until [ "$RESPONSE" = "completed" ]
do
  RESPONSE=$(aws ec2 describe-export-tasks --export-task-ids $EXPORT_ID | jq -r '.ExportTasks[].State')
  if [ "$RESPONSE" != "exit" ]; then
    echo "OVA export in progress..."
    sleep 30
  fi
done

echo "Upload Completed !!!"

BUCKET=$(cat packer/ova-export.json | jq -r .S3Bucket)
OBJECT="$(aws s3 ls $BUCKET/vms/SOW-REST/ | sort | tail -n 1 | awk '{print $4}')"
aws s3 mv s3://${BUCKET}/vms/k8-rebuild-folder-to-folder/${OBJECT} s3://${BUCKET}/vms/k8-rebuild-folder-to-folder/${OVA_NAME}

