using System;
using System.Reflection;
using System.Text.Json;
using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Configuration
{
    [TestClass]
    public class ConfigurationFileProviderTests
    {
        [TestMethod]
        public void ConfigurationFileProvider_AppliesGlobalAndNamespaceOverrides()
        {
            var provider = new ConfigurationProvider();

            var payload = new HierarchicalConfigurationFile
            {
                Global = new OperationConfiguration { SamplingRate = 0.2 },
                Namespaces =
                {
                    ["HVO.Enterprise.Telemetry.Tests.*"] = new OperationConfiguration { Enabled = false }
                }
            };

            ConfigurationFileProvider.ApplyTo(provider, payload, ConfigurationSourceKind.File);

            var effective = provider.GetEffectiveConfiguration(typeof(FileConfiguredService));

            Assert.AreEqual(0.2, effective.SamplingRate);
            Assert.AreEqual(false, effective.Enabled);
        }

        [TestMethod]
        public void ConfigurationFileProvider_AppliesTypeAndMethodOverrides()
        {
            var provider = new ConfigurationProvider();

            var payload = new HierarchicalConfigurationFile
            {
                Types =
                {
                    [typeof(FileConfiguredService).AssemblyQualifiedName!] = new OperationConfiguration { SamplingRate = 0.4 }
                },
                Methods =
                {
                    [typeof(FileConfiguredService).AssemblyQualifiedName + "::Run"] = new OperationConfiguration { SamplingRate = 0.9 }
                }
            };

            ConfigurationFileProvider.ApplyTo(
                provider,
                payload,
                ConfigurationSourceKind.File,
                ResolveType,
                ResolveMethod);

            var method = typeof(FileConfiguredService).GetMethod(nameof(FileConfiguredService.Run));
            var effective = provider.GetEffectiveConfiguration(typeof(FileConfiguredService), method);

            Assert.AreEqual(0.9, effective.SamplingRate);
        }

        [TestMethod]
        public void ConfigurationFileProvider_LoadFromJson_ValidatesPayload()
        {
            var payload = new HierarchicalConfigurationFile
            {
                Global = new OperationConfiguration { SamplingRate = 0.5 }
            };

            var json = JsonSerializer.Serialize(payload);
            var parsed = ConfigurationFileProvider.LoadFromJson(json);

            Assert.IsNotNull(parsed);
            Assert.IsNotNull(parsed.Global);
            Assert.AreEqual(0.5, parsed.Global!.SamplingRate);
        }

        [TestMethod]
        public void ConfigurationFileProvider_LoadFromJson_HandlesNullCollections()
        {
            var json = @"{""Global"":{""SamplingRate"":0.3},""Namespaces"":null,""Types"":null,""Methods"":null}";
            var parsed = ConfigurationFileProvider.LoadFromJson(json);

            Assert.IsNotNull(parsed);
            Assert.IsNotNull(parsed.Global);
            Assert.AreEqual(0.3, parsed.Global!.SamplingRate);
            Assert.IsNotNull(parsed.Namespaces);
            Assert.IsNotNull(parsed.Types);
            Assert.IsNotNull(parsed.Methods);
        }

        [TestMethod]
        public void ConfigurationFileProvider_ApplyTo_SkipsInvalidTypeKeys()
        {
            var provider = new ConfigurationProvider();

            var payload = new HierarchicalConfigurationFile
            {
                Types =
                {
                    ["InvalidType"] = new OperationConfiguration { SamplingRate = 0.4 },
                    [typeof(FileConfiguredService).AssemblyQualifiedName!] = new OperationConfiguration { SamplingRate = 0.5 }
                }
            };

            ConfigurationFileProvider.ApplyTo(
                provider,
                payload,
                ConfigurationSourceKind.File,
                ResolveType,
                ResolveMethod);

            var effective = provider.GetEffectiveConfiguration(typeof(FileConfiguredService));
            Assert.AreEqual(0.5, effective.SamplingRate);
        }

        [TestMethod]
        public void ConfigurationFileProvider_ApplyTo_SkipsInvalidMethodKeys()
        {
            var provider = new ConfigurationProvider();

            var payload = new HierarchicalConfigurationFile
            {
                Methods =
                {
                    ["InvalidKey"] = new OperationConfiguration { SamplingRate = 0.7 },
                    ["InvalidType::Method"] = new OperationConfiguration { SamplingRate = 0.8 },
                    [typeof(FileConfiguredService).AssemblyQualifiedName + "::InvalidMethod"] = new OperationConfiguration { SamplingRate = 0.9 }
                }
            };

            ConfigurationFileProvider.ApplyTo(
                provider,
                payload,
                ConfigurationSourceKind.File,
                ResolveType,
                ResolveMethod);

            var method = typeof(FileConfiguredService).GetMethod(nameof(FileConfiguredService.Run));
            var effective = provider.GetEffectiveConfiguration(typeof(FileConfiguredService), method);
            
            // Should use default since no valid method config was applied
            Assert.AreEqual(1.0, effective.SamplingRate);
        }

        [TestMethod]
        public void ConfigurationFileProvider_ApplyTo_ParsesMethodKeysCorrectly()
        {
            var provider = new ConfigurationProvider();

            var typeName = typeof(FileConfiguredService).AssemblyQualifiedName!;
            var payload = new HierarchicalConfigurationFile
            {
                Methods =
                {
                    [typeName + "::Run"] = new OperationConfiguration { SamplingRate = 0.6, Enabled = false }
                }
            };

            ConfigurationFileProvider.ApplyTo(
                provider,
                payload,
                ConfigurationSourceKind.File,
                ResolveType,
                ResolveMethod);

            var method = typeof(FileConfiguredService).GetMethod(nameof(FileConfiguredService.Run));
            var effective = provider.GetEffectiveConfiguration(typeof(FileConfiguredService), method);

            Assert.AreEqual(0.6, effective.SamplingRate);
            Assert.AreEqual(false, effective.Enabled);
        }

        [TestMethod]
        public void ConfigurationFileProvider_ApplyTo_RespectsTypeAndMethodResolution()
        {
            var provider = new ConfigurationProvider();
            var resolveTypeCalled = false;
            var resolveMethodCalled = false;

            var payload = new HierarchicalConfigurationFile
            {
                Types =
                {
                    ["TestType"] = new OperationConfiguration { SamplingRate = 0.3 }
                },
                Methods =
                {
                    ["TestType::TestMethod"] = new OperationConfiguration { SamplingRate = 0.4 }
                }
            };

            ConfigurationFileProvider.ApplyTo(
                provider,
                payload,
                ConfigurationSourceKind.File,
                typeName =>
                {
                    resolveTypeCalled = true;
                    return typeName == "TestType" ? typeof(FileConfiguredService) : null;
                },
                (type, methodName) =>
                {
                    resolveMethodCalled = true;
                    return type == typeof(FileConfiguredService) && methodName == "TestMethod"
                        ? type.GetMethod(nameof(FileConfiguredService.Run))
                        : null;
                });

            Assert.IsTrue(resolveTypeCalled);
            Assert.IsTrue(resolveMethodCalled);
        }

        [TestMethod]
        public void ConfigurationFileProvider_LoadFromFile_ThrowsOnNullPath()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => ConfigurationFileProvider.LoadFromFile(null!));
        }

        [TestMethod]
        public void ConfigurationFileProvider_LoadFromJson_ThrowsOnNullJson()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => ConfigurationFileProvider.LoadFromJson(null!));
        }

        [TestMethod]
        public void ConfigurationFileProvider_ApplyTo_ThrowsOnNullProvider()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                var payload = new HierarchicalConfigurationFile();
                ConfigurationFileProvider.ApplyTo(null!, payload);
            });
        }

        [TestMethod]
        public void ConfigurationFileProvider_ApplyTo_ThrowsOnNullFile()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                var provider = new ConfigurationProvider();
                ConfigurationFileProvider.ApplyTo(provider, null!);
            });
        }

        private static Type? ResolveType(string typeName)
        {
            return typeof(FileConfiguredService).AssemblyQualifiedName == typeName
                ? typeof(FileConfiguredService)
                : null;
        }

        private static MethodInfo? ResolveMethod(Type type, string methodName)
        {
            return type == typeof(FileConfiguredService)
                ? type.GetMethod(methodName)
                : null;
        }

        private sealed class FileConfiguredService
        {
            public void Run()
            {
            }
        }
    }
}
