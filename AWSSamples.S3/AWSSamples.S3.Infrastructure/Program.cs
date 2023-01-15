using Pulumi;
using Pulumi.Aws.S3;
using Pulumi.Aws.S3.Inputs;
using Aws = Pulumi.Aws;

return await Deployment.RunAsync(async () =>
{
    var config = new Config();
    var domain = config.Require("domain");
    var emails = config.Require("emails");
    var nameBase = "aws-samples-s3-blazor";

    var bucket = new Aws.S3.Bucket($"{nameBase}-bucket", new BucketArgs
    {
        BucketName = $"{nameBase}-bucket",
        Website = new BucketWebsiteArgs
        {
            IndexDocument = "index.html"
        }
    });
    
    var cost = new Aws.Budgets.Budget($"{nameBase}-cost", new()
    {
        Name = $"{nameBase}-cost",
        BudgetType = "COST",
        LimitAmount = "10",
        LimitUnit = "USD",
        TimeUnit = "MONTHLY",
        Notifications = new[]
        {
            new Aws.Budgets.Inputs.BudgetNotificationArgs
            {
                ComparisonOperator = "GREATER_THAN",
                NotificationType = "FORECASTED",
                SubscriberEmailAddresses = emails.Split(","),
                Threshold = 50,
                ThresholdType = "PERCENTAGE",
            },
            new Aws.Budgets.Inputs.BudgetNotificationArgs
            {
                ComparisonOperator = "GREATER_THAN",
                NotificationType = "ACTUAL",
                SubscriberEmailAddresses = emails.Split(","),
                Threshold = 50,
                ThresholdType = "PERCENTAGE",
            },  
            new Aws.Budgets.Inputs.BudgetNotificationArgs
            {
                ComparisonOperator = "GREATER_THAN",
                NotificationType = "ACTUAL",
                SubscriberEmailAddresses = emails.Split(","),
                Threshold = 80,
                ThresholdType = "PERCENTAGE",
            },
        }
    });

    // Export the name of the bucket
    return new Dictionary<string, object?>
    {
        ["BucketArn"] = bucket.Arn,
    };
});