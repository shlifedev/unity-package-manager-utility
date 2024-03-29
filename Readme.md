# PackageManagerUtility
* [dev](https://github.com/shlifedev/unity-package-manager-utility/tree/dev)
 In this branch, removed the UniTask-specific dependencies, so all you need to do is add the editorcoroutine to your package.json to use it. If you want UniTask functionality, add the `UNITASK` define to your buildsettings. 


The Unity Engine automatically manages your package dependencies with package.json and manifest.json.

You may need to use the functionality of UnityEditor.PackageManager to manage your own packages. However, Unity does not support a `scoped registry` for unknown reasons, and the API is set to Internal, so you cannot access the API in the usual way.

Therefore, I've written a hook for ScopedRegistry and will gradually update this repo to add temporary support for external registries like openupm, git, etc.

But for now, it only provides the hooks API. 

## Requirement
- UniTask

## How to use
You can easily access all APIs through the `PackageManagerUtility.cs` You will need to install `unitask`.
```csharp
 class Example 
    {
        async UniTask YourFunction()
        {
            // You can add scoped registry with code.
            await PackageManagerUtility.AddScopeAndRegistry("openupm", "https://package.openupm.com",
                new[] { "com.shlifedev.ugs" });

            // You can get scoped registry in project
            var registires = await PackageManagerUtility.GetRegistries();
            foreach (var reg in registires)
            {
                Debug.Log("Registry : " + reg.name);
            }
            
            
            // You can get scopes and, update scopes
            var openUpmScopes = await PackageManagerUtility.GetScopes("openupm");
            // You can merging scope.
            var newScopes = PackageManagerUtility.MergeScopes(openUpmScopes, new[] { "com.cysharp.unitask" });
            // Update Registry.
            await PackageManagerUtility.UpdateScopedRegistry("openupm", "https://package.openupm.com", newScopes);
            
            
            // You can find missing dependencies and adding.
            MissingDependency[] missingDependencies = await PackageManagerUtility.GetMissingDependency();
            foreach (var missing in missingDependencies)
            {

             // but it's support only github or unity registry. if you need adding other package registry dependency then using UpdateScopedRegistry function.
                await PackageManagerUtility.AddDependency(missing);
            }
            
         
            // ... etc
        }
    }
```

## You can import this source your project.
You can import this your project and deploy. 

### Thank
I reference this gist. [here](https://gist.github.com/Thaina/eec5752b25f7bfd3737f7dd9ed2fa53c)
