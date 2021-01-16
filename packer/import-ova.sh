# Uncomment to import OVA to AWS
OVA_NAME=${OVA_NAME:-$1}
cat > packer/containers.json <<EOF
[
    {
        "Description": "filedrop OVA",
        "Format": "ova",
        "UserBucket": {
            "S3Bucket": "glasswall-sow-ova",
            "S3Key": "vms/k8-rebuild-folder-to-folder/${OVA_NAME}"
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
  if [ "$RESPONSE" == "deleted" ]; then
    echo "Failed to import OVA"
    echo "OVA Import status is $RESPONSE"
    echo "Status message is $StatusMessage"
    exit -1
  elif [ "$RESPONSE" == "active" ]; then
    echo "OVA Import status is $RESPONSE"
    echo "Status message is $StatusMessage"
    sleep 30
  fi
done
