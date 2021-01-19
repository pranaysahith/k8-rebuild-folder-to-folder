### Build Images using Packer

## Prerequisites

- Install packer from https://learn.hashicorp.com/tutorials/packer/getting-started-install
- Install AWS CLI from https://docs.aws.amazon.com/cli/latest/userguide/install-cliv2.html
- AWS Access Key and Secret key of an IAM user with access to ec2 and read/write access to s3

## Build AMI

- Clone the k8-rebuild repo - 
    ```
    git clone https://github.com/k8-proxy/k8-rebuild-folder-to-folder.git
    ```
- Build AMI by running below command from `k8-rebuild-folder-to-folder` folder after updating <AWS_ACCESS_KEY> and <AWS_SECRET_ACCESS_KEY> values in the below command. Update the region(eu-west-1) depending on where the AMI needs to be created. Pass different github_sha everytime we run the command.
    ```
    packer build -color=false -on-error=cleanup -var github_sha=01-07-2021 -var vm_name=k8-rebuild -var region=eu-west-1 -var aws_access_key=<AWS_ACCESS_KEY> -var aws_secret_key=<AWS_SECRET_ACCESS_KEY> packer/aws-ova.json
    ```
- Optional: Packer will create a temporary security group but you can create one on AWS and pass it to the above command as a variable
    ```
    -var security_group_ids=<SECURITY_GROUP_ID>
    ```
- At the end of the process, the AMI ID and s3 path of OVA will be displayed.
- Import the OVA to AWS, by running below command:
    ```
    ./packer/import-ova.sh <S3_PATH_OF_OVA>
    ```
- Create and attache EBS volume by running below command:
    ```
    ./packer/attch-volume.sh <INSTANCE_ID>
    ```
- Mount the volume on ec2
    ```
    ./packer/mount-volume.sh </mount/path>
    ```
- The mount path created above can be used in the docker-compose to attach it to the file handler docker container.
