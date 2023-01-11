using Pulumi;
using System.Collections.Generic;
using Aws = Pulumi.Aws;

return await Deployment.RunAsync(() =>
{
    var config = new Config();
    var domain = config.Require("domain");
    var nameBase = "aws-samples-lambda";

    var iamForLambda = new Aws.Iam.Role($"{nameBase}-iamForLambda", new()
    {
        Name = $"{nameBase}-exampleFunction",
        AssumeRolePolicy = @"{
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
}
",
    });

    var function = new Aws.Lambda.Function($"{nameBase}-exampleFunction", new()
    {
        Name = $"{nameBase}-exampleFunction",
        Code = new FileArchive("lambda_function_payload.zip"),
        Role = iamForLambda.Arn,
        Handler = "AWSSamples.Lambda.Web",
        Runtime = "dotnet6",
    });

    var cert = Aws.Acm.GetCertificate.Invoke(new()
    {
        Domain = domain
    });

    var api = new Aws.ApiGatewayV2.Api($"{nameBase}-example-api", new()
    {
        Name = $"{nameBase}-example-api",
        ProtocolType = "HTTP",
    });

    var integration = new Aws.ApiGatewayV2.Integration($"{nameBase}-gateway-integration", new()
    {
        ApiId = api.Id,
        IntegrationType = "AWS_PROXY",
        ConnectionType = "INTERNET",
        ContentHandlingStrategy = "CONVERT_TO_TEXT",
        Description = "Lambda example",
        IntegrationMethod = "ANY",
        IntegrationUri = function.InvokeArn,
        PassthroughBehavior = "WHEN_NO_MATCH",
    });

    var stage = new Aws.ApiGatewayV2.Stage($"{nameBase}-gateway-stage", new()
    {
        Name = $"{nameBase}-gateway-stage",
        ApiId = api.Id,
    });

    var route = new Aws.ApiGatewayV2.Route($"{nameBase}-gateway-route", new()
    {
        ApiId = api.Id,
        RouteKey = "$default",
        Target = integration.Id.Apply(id => $"integrations/{id}")
    });

    var domainName = new Aws.ApiGatewayV2.DomainName($"{nameBase}-gateway-domainname", new()
    {
        Domain = domain,
        DomainNameConfiguration = new Aws.ApiGatewayV2.Inputs.DomainNameDomainNameConfigurationArgs
        {
            CertificateArn = cert.Apply(c => c.Arn.ToString()),
            EndpointType = "REGIONAL",
            SecurityPolicy = "TLS_1_2",
        },
    });

    var mapping = new Aws.ApiGatewayV2.ApiMapping($"{nameBase}-gateway-mapping", new()
    {
        ApiId = api.Id,
        DomainName = domainName.Id,
        Stage = stage.Id,
    });

    // Export the name of the bucket
    return new Dictionary<string, object?>
    {
        ["API"] = api.Name,
        ["Stage"] = stage.Name,
        ["StageInvokeUrl"] = stage.InvokeUrl,
    };
});