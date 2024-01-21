#if UNITASK
using Cysharp.Threading.Tasks;
using UnityEditor.PackageManager;

namespace LD.Editor
{
    public static partial class PackageManagerUtility
    {
        public static async UniTask AddDependency(MissingDependency missing)
        {
            await AddDependencyCoroutine(missing).ToUniTask();
        }
        
        public static async UniTask<MissingDependency[]> GetMissingDependency()
        {
            MissingDependency[] missingDependencies = null;
            await GetMissingDependencyCoroutine(result => { missingDependencies = result; }).ToUniTask();
            return missingDependencies;
        }
        
        // UniTask version using ToUniTask()
        public static async UniTask<string[]> GetScopes(string registryName)
        {
            string[] scopes = null;
            await GetScopesCoroutine(registryName, result => { scopes = result; }).ToUniTask();
            return scopes;
        }
        
        public static async UniTask<StatusCode> UpdateScopedRegistry(string registryName, string url, string[] scopes)
        {
            StatusCode statusCode = StatusCode.Failure;
            await UpdateScopedRegistryCoroutine(registryName, url, scopes, result => { statusCode = result;}).ToUniTask();
            return statusCode;
        }
        
        
        public static async UniTask<StatusCode> AddScopeAndRegistry(string registryName, string url, string[] scopes)
        {
            StatusCode statusCode = StatusCode.Failure;
            await AddScopeAndRegistryCoroutine(registryName, url, scopes, result => { statusCode = result; }).ToUniTask();
            return statusCode;
        }

        public static async UniTask<string> GetRegistryId(string registryName)
        {
            string registryId = null;
            await GetRegistryIdCoroutine(registryName, result => { registryId = result; }).ToUniTask();
            return registryId;
        }
        
        public static async UniTask<RegistryInfo[]> GetRegistries()
        {
            RegistryInfo[] registries = null;
            await GetRegistriesCoroutine(result => { registries = result; }).ToUniTask();
            return registries;
        }
    }
}
#endif