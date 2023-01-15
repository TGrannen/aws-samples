using Pulumi;
using Pulumi.Aws.Acm;
using Pulumi.Aws.S3;
using Pulumi.Aws.S3.Inputs;
using Aws = Pulumi.Aws;

return await Deployment.RunAsync(async () =>
{
    var config = new Config();
    var domain = config.Require("domain");
    var emails = config.Require("emails");
    var nameBase = "aws-samples-s3-blazor";

    var bucket = new Bucket($"{nameBase}-bucket", new BucketArgs
    {
        BucketName = $"{nameBase}-bucket",
        // Acl = "public-read",
        Policy = File.ReadAllText("policy.json"),
        Website = new BucketWebsiteArgs
        {
            IndexDocument = "index.html",
            ErrorDocument = "index.html",
        }
    });

    var example = new BucketWebsiteConfigurationV2($"{nameBase}-bucket-website-config", new()
    {
        Bucket = bucket.BucketName,
        IndexDocument = new BucketWebsiteConfigurationV2IndexDocumentArgs
        {
            Suffix = "index.html",
        },
        ErrorDocument = new BucketWebsiteConfigurationV2ErrorDocumentArgs
        {
            Key = "index.html",
        }
    });

    // var cert = GetCertificate.Invoke(new GetCertificateInvokeArgs
    // {
    //     Domain = domain,
    // }, new InvokeOptions
    // {
    //     Provider = new Aws.Provider("us-east-1-provider", new Aws.ProviderArgs { Region = "us-east-1" })
    // });

    var s3OriginId = "s3-blazor-origin";

    var s3Distribution = new Aws.CloudFront.Distribution($"{nameBase}-distribution", new()
    {
        Origins = new[]
        {
            new Aws.CloudFront.Inputs.DistributionOriginArgs
            {
                DomainName = bucket.BucketRegionalDomainName,
                OriginId = s3OriginId,
            }
        },
        Enabled = true,
        IsIpv6Enabled = true,
        Comment = "Some comment",
        DefaultRootObject = "index.html",
        // LoggingConfig = new Aws.CloudFront.Inputs.DistributionLoggingConfigArgs
        // {
        //     IncludeCookies = false,
        //     Bucket = "mylogs.s3.amazonaws.com",
        //     Prefix = "myprefix",
        // },
        // Aliases = new[]
        // {
        //     domain
        // },
        DefaultCacheBehavior = new Aws.CloudFront.Inputs.DistributionDefaultCacheBehaviorArgs
        {
            AllowedMethods = new[]
            {
                "GET",
                "HEAD",
                "OPTIONS",
            },
            CachedMethods = new[]
            {
                "GET",
                "HEAD",
            },
            TargetOriginId = s3OriginId,
            ForwardedValues = new Aws.CloudFront.Inputs.DistributionDefaultCacheBehaviorForwardedValuesArgs
            {
                QueryString = false,
                Cookies = new Aws.CloudFront.Inputs.DistributionDefaultCacheBehaviorForwardedValuesCookiesArgs
                {
                    Forward = "none",
                },
            },
            ViewerProtocolPolicy = "allow-all",
            MinTtl = 0,
            DefaultTtl = 3600,
            MaxTtl = 86400,
        },
        PriceClass = "PriceClass_100",
        Restrictions = new Aws.CloudFront.Inputs.DistributionRestrictionsArgs
        {
            GeoRestriction = new Aws.CloudFront.Inputs.DistributionRestrictionsGeoRestrictionArgs
            {
                RestrictionType = "whitelist",
                Locations = new[]
                {
                    "US",
                    "CA",
                },
            },
        },
        Tags =
        {
            { "Environment", "production" },
        },
        ViewerCertificate = new Aws.CloudFront.Inputs.DistributionViewerCertificateArgs
        {
            CloudfrontDefaultCertificate = true
        },
        // ViewerCertificate = new Aws.CloudFront.Inputs.DistributionViewerCertificateArgs
        // {
        //     AcmCertificateArn = cert.Apply(x => x.Arn),
        //     SslSupportMethod = "sni-only"
        // },
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