using Pulumi;
using Pulumi.Aws.Lambda;
using Aws = Pulumi.Aws;
using S3 = Pulumi.Aws.S3;

namespace AWSSamples.Lambda.Infrastructure.Helpers;

/// <summary>
/// Original: https://nodogmablog.bryanhogan.net/2021/03/c-and-aws-lambdas-part-5-updating-the-zip-in-s3-and-updating-the-running-lambda-with-pulumi-iac/
/// </summary>
internal class MyStack
{
    public MyStack(string resourcePrefix, string zipPath)
    {
        var lambdaHelloWorldRole = new Aws.Iam.Role($"{resourcePrefix}-lambdaRole", new Aws.Iam.RoleArgs
        {
            AssumeRolePolicy =
                @"{
    ""Version"": ""2012-10-17"",
    ""Statement"": [
        {
        ""Action"": ""sts:AssumeRole"",
        ""Principal"": {
            ""Service"": ""lambda.amazonaws.com""
        },
        ""Effect"": ""Allow"",
        ""Sid"": """"
        }
    ]
}",
        });

        var lambdaUpdateRole = new Aws.Iam.Role($"{resourcePrefix}-lambdaUpdateRole", new Aws.Iam.RoleArgs
        {
            AssumeRolePolicy =
                @"{
    ""Version"": ""2012-10-17"",
    ""Statement"": [
        {
        ""Action"": ""sts:AssumeRole"",
        ""Principal"": {
            ""Service"": ""lambda.amazonaws.com""
        },
        ""Effect"": ""Allow"",
        ""Sid"": """"
        }
    ]
}",
        });

        // gives the Lambda permissions to other Lambdas and S3 - too many permissions, but this is a demo.
        var lambdaUpdatePolicy = new Aws.Iam.Policy($"{resourcePrefix}-s3-lambda_Policy", new Aws.Iam.PolicyArgs
        {
            PolicyDocument =
                @"{
    ""Version"": ""2012-10-17"",
    ""Statement"": [
        {
            ""Sid"": """",
            ""Effect"": ""Allow"",
            ""Action"": [
                ""s3:*"",
                ""logs:*"",
                ""lambda:*""
            ],
            ""Resource"": ""*""
        }
    ]
}"
        });

        // attach a simple policy to the hello world Lambda.
        var lambdaHelloWorldAttachment = new Aws.Iam.PolicyAttachment($"{resourcePrefix}-lambdaHelloWorldPolicyAttachment",
            new Aws.Iam.PolicyAttachmentArgs
            {
                Roles =
                {
                    lambdaHelloWorldRole.Name
                },
                PolicyArn = Aws.Iam.ManagedPolicy.AWSLambdaBasicExecutionRole.ToString(),
            });

        // attach the custom policy to the role that runs the update Lambda.
        var lambdaUpdateAttachment = new Aws.Iam.PolicyAttachment($"{resourcePrefix}-lambdaUpdatePolicyAttachment",
            new Aws.Iam.PolicyAttachmentArgs
            {
                Roles =
                {
                    lambdaUpdateRole.Name
                },
                PolicyArn = lambdaUpdatePolicy.Arn,
            });

        var s3Bucket = new S3.Bucket($"{resourcePrefix}-s3Bucket", new S3.BucketArgs
        {
            BucketName = "pulumi-hello-world-auto-update-s3-bucket",
            Versioning = new S3.Inputs.BucketVersioningArgs
            {
                Enabled = true,
            },
            Acl = "private"
        });

        var s3BucketObject = new S3.BucketObject($"{resourcePrefix}-zipFile", new S3.BucketObjectArgs
        {
            Bucket = s3Bucket.BucketName.Apply(name => name),
            Acl = "private",
            Source = new FileArchive(zipPath),
            Key = "helloworld.zip"
        });

        // this is the Lambda that runs .NET code
        var lambdaHelloWorldFunction = new Aws.Lambda.Function($"{resourcePrefix}-lambdaHelloWorldFunction", new Aws.Lambda.FunctionArgs
        {
            Handler = "AWSSamples.Lambda.Web",
            MemorySize = 128,
            Role = lambdaHelloWorldRole.Arn,
            Runtime = Aws.Lambda.Runtime.Dotnet6,
            S3Bucket = s3Bucket.BucketName,
            S3Key = s3BucketObject.Key
        });

        // this is the Lambda triggered by an upload to S3 and replaces the zip in the above Lambda
        var lambdaUpdateFunction = new Aws.Lambda.Function($"{resourcePrefix}-lambdaUpdateFunction", new Aws.Lambda.FunctionArgs
        {
            Handler = "index.handler",
            MemorySize = 128,
            Publish = false,
            ReservedConcurrentExecutions = -1,
            Role = lambdaUpdateRole.Arn,
            Runtime = Aws.Lambda.Runtime.NodeJS14dX,
            Timeout = 4,
            Code = new FileArchive("./Lambdas/index.zip"),
            Environment = new Aws.Lambda.Inputs.FunctionEnvironmentArgs
            {
                Variables = new InputMap<string>
                {
                    { "s3Bucket", s3Bucket.BucketName }, { "s3Key", "helloworld.zip" },
                    { "functionToUpdate", lambdaHelloWorldFunction.Name }
                }
            }
        });

        var s3BucketPermissionToCallLambda = new Aws.Lambda.Permission($"{resourcePrefix}_S3BucketPermissionToCallLambda",
            new Aws.Lambda.PermissionArgs
            {
                Action = "lambda:InvokeFunction",
                Function = lambdaUpdateFunction.Arn,
                Principal = "s3.amazonaws.com",
                SourceArn = s3Bucket.Arn,
            });

        var bucketNotification = new S3.BucketNotification($"{resourcePrefix}_S3BucketNotification", new Aws.S3.BucketNotificationArgs
        {
            Bucket = s3Bucket.Id,
            LambdaFunctions =
            {
                new Aws.S3.Inputs.BucketNotificationLambdaFunctionArgs
                {
                    LambdaFunctionArn = lambdaUpdateFunction.Arn,
                    Events =
                    {
                        "s3:ObjectCreated:*",
                    },
                }
            },
        }, new CustomResourceOptions
        {
            DependsOn =
            {
                s3BucketPermissionToCallLambda,
            },
        });

        // keep the contents bucket private
        var bucketPublicAccessBlock = new S3.BucketPublicAccessBlock($"{resourcePrefix}_PublicAccessBlock",
            new S3.BucketPublicAccessBlockArgs
            {
                Bucket = s3Bucket.Id,
                BlockPublicAcls = false, // leaving these two false because I need them this way 
                IgnorePublicAcls = false, // for a post about GitHub Actions that I'm working on
                BlockPublicPolicy = true,
                RestrictPublicBuckets = true
            });

        this.LambdaUpdateFunctionName = lambdaUpdateFunction.Name;
        this.LambdaHelloWorldFunctionName = lambdaHelloWorldFunction.Name;
        this.LambdaHelloWorldFunction = lambdaHelloWorldFunction;
        this.S3Bucket = s3Bucket.BucketName;
        this.S3Key = s3BucketObject.Key;
    }

    [Output] public Output<string> LambdaUpdateFunctionName { get; set; }

    [Output] public Output<string> LambdaHelloWorldFunctionName { get; set; }
    [Output] public Function LambdaHelloWorldFunction { get; set; }

    [Output] public Output<string> S3Bucket { get; set; }

    [Output] public Output<string> S3Key { get; set; }
}