using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerService;
using k8s;
using k8s.KubeConfigModels;
using static Learning.Common.Constants;
using Azure.Core;
using Azure.ResourceManager.ContainerService.Models;

namespace Learning.API.AKS
{
    public class ListNamespaces
    {
        private readonly IConfiguration _configuration;

        public ListNamespaces(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<string> RunAsync()
        {
            try
            {
                var aksName = ValidateNullableProperty(_configuration[AKSName], AKSName);

                var subscriptionId = ValidateNullableProperty(_configuration[AzureSubscriptionId], AzureSubscriptionId); var aksClientId = string.Empty;

                var tokenCredentialsOptions = new DefaultAzureCredentialOptions();

                var isLocalEnvironment = string.IsNullOrEmpty(_configuration[ASPNETCOREENVIRONMENT]) || _configuration[ASPNETCOREENVIRONMENT].ToLower() == Local.ToLower();

                if (isLocalEnvironment)
                {
                    Console.WriteLine("Local Environment");
                    tokenCredentialsOptions.ExcludeAzureCliCredential = false;
                }
                else
                {
                    Console.WriteLine("Non Local Environment");
                    aksClientId = ValidateNullableProperty(_configuration[AKSClientId], AKSClientId);
                    tokenCredentialsOptions.ExcludeManagedIdentityCredential = false;
                    tokenCredentialsOptions.ManagedIdentityClientId = aksClientId;
                }

                tokenCredentialsOptions.TenantId = _configuration[AzureTenantId];

                var tokenCredentials = new DefaultAzureCredential(tokenCredentialsOptions);

                var armClient = new ArmClient(tokenCredentials);

                var subscriptions = armClient.GetSubscriptions();

                Console.WriteLine("Sub Id : " + subscriptions.ToList()[0].Id);

                var foundSubscription = subscriptions.Single(x => x.Data.Id.SubscriptionId == subscriptionId);
                
                var clusters = foundSubscription.GetContainerServiceManagedClustersAsync();

                ContainerServiceManagedClusterResource foundCluster = default!;

                await foreach (var cluster in clusters)
                {
                    if (cluster.Data.Name == _configuration[AKSName])
                    {
                        foundCluster = cluster;
                        Console.WriteLine("Cluster Namem : " + foundCluster.Data.Name);
                        break;
                    }
                }

                if (foundCluster == default!)
                    return "No Cluster Found";

                var clusterUser = await foundCluster.GetClusterUserCredentialsAsync(format: KubeConfigFormat.Exec).ConfigureAwait(false);

                var stream = new MemoryStream(clusterUser.Value.Kubeconfigs.SelectMany(x => x.Value).ToArray());

                var k8sConfig = KubernetesYaml.Deserialize<K8SConfiguration>(stream);

                Console.WriteLine("Serialized yaml successfully");

                k8sConfig.Users.ToList()[0].UserCredentials.ExternalExecution = new ExternalExecution()
                {
                    ApiVersion = ExternalExecutionApiVersion,
                    Arguments = new List<string>() { GetTokenString, Login, MSI, ServerId, AKSADApplicationId, ClientId, aksClientId },
                    Command = KubeLogin
                };

                if (isLocalEnvironment)
                    k8sConfig.Users.ToList()[0].UserCredentials.ExternalExecution.Arguments = new List<string>() { GetTokenString, Login, Azurecli, ServerId, AKSADApplicationId };

                Console.WriteLine("Successfully set external execution to the kube config.");

                var kubeConfig = KubernetesClientConfiguration.BuildConfigFromConfigObject(k8sConfig);

                var client = new Kubernetes(kubeConfig);

                var namespaces = await client.ListNamespaceAsync();

                Console.WriteLine("Namespaces are .........");

                Console.WriteLine(string.Join(",", namespaces.Items.Select(i => i.Metadata.Name)));

                Console.WriteLine("Completed Fetching Namespaces....");

                return string.Join(",", namespaces.Items.Select(i => i.Metadata.Name));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return ex.Message + ex.StackTrace;
            }
        }

        #region private-methods
        private static string ValidateNullableProperty(string propertyValue, string propertyName)
        {
            return  string.IsNullOrEmpty(propertyValue) ? throw new InvalidOperationException($"{propertyName} configuration value cannot be null or empty.") : propertyValue;
        }
        #endregion
    }
}
