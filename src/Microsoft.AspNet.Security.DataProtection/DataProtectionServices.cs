﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Microsoft.AspNet.Security.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNet.Security.DataProtection.Cng;
using Microsoft.AspNet.Security.DataProtection.Dpapi;
using Microsoft.AspNet.Security.DataProtection.KeyManagement;
using Microsoft.AspNet.Security.DataProtection.Repositories;
using Microsoft.AspNet.Security.DataProtection.XmlEncryption;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Security.DataProtection
{
    public static class DataProtectionServices
    {
        public static IEnumerable<IServiceDescriptor> GetDefaultServices()
        {
            return GetDefaultServices(new Configuration());
        }

        public static IEnumerable<IServiceDescriptor> GetDefaultServices(IConfiguration configuration)
        {
            var describe = new ServiceDescriber(configuration);

            List<IServiceDescriptor> descriptors = new List<IServiceDescriptor>();
            descriptors.AddRange(OptionsServices.GetDefaultServices(configuration));
            descriptors.AddRange(OSVersionUtil.IsBCryptOnWin7OrLaterAvailable()
                ? GetDefaultServicesWindows(describe)
                : GetDefaultServicesNonWindows(describe));
            return descriptors;
        }

        private static IEnumerable<IServiceDescriptor> GetDefaultServicesNonWindows(ServiceDescriber describe)
        {
            // If we're not running on Windows, we can't use CNG.

            // TODO: Replace this with something else. Mono's implementation of the
            // DPAPI routines don't provide authenticity.
            return new[]
            {
                describe.Instance<IDataProtectionProvider>(new DpapiDataProtectionProvider(DataProtectionScope.CurrentUser))
            };
        }

        private static IEnumerable<IServiceDescriptor> GetDefaultServicesWindows(ServiceDescriber describe)
        {
            List<ServiceDescriptor> descriptors = new List<ServiceDescriptor>();

            // Are we running in Azure Web Sites?
            DirectoryInfo azureWebSitesKeysFolder = TryGetKeysFolderForAzureWebSites();
            if (azureWebSitesKeysFolder != null)
            {
                // We'll use a null protector at the moment until the
                // cloud DPAPI service comes online.
                descriptors.AddRange(new[]
                {
                    describe.Singleton<IXmlEncryptor,NullXmlEncryptor>(),
                    describe.Instance<IXmlRepository>(new FileSystemXmlRepository(azureWebSitesKeysFolder))
                });
            }
            else
            {
                // Are we running with the user profile loaded?
                DirectoryInfo localAppDataKeysFolder = TryGetLocalAppDataKeysFolderForUser();
                if (localAppDataKeysFolder != null)
                {
                    descriptors.AddRange(new[]
                    {
                        describe.Singleton<IXmlEncryptor, DpapiXmlEncryptor>(),
                        describe.Instance<IXmlRepository>(new FileSystemXmlRepository(localAppDataKeysFolder))
                    });
                }
                else
                {
                    // Are we running with no user profile (e.g., IIS service)?
                    // Fall back to DPAPI for now.
                    // TODO: We should use the IIS auto-gen reg keys as our repository.
                    return new[] {
                        describe.Instance<IDataProtectionProvider>(new DpapiDataProtectionProvider(DataProtectionScope.LocalMachine))
                    };
                }
            }

            // We use CNG CBC + HMAC by default.
            descriptors.AddRange(new[]
            {
                describe.Singleton<IAuthenticatedEncryptorConfigurationFactory, CngCbcAuthenticatedEncryptorConfigurationFactory>(),
                describe.Singleton<ITypeActivator, TypeActivator>(),
                describe.Singleton<IKeyManager, XmlKeyManager>(),
                describe.Singleton<IDataProtectionProvider, DefaultDataProtectionProvider>()
            });

            return descriptors;
        }

        private static DirectoryInfo TryGetKeysFolderForAzureWebSites()
        {
            // There are two environment variables we care about.
            if (String.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")))
            {
                return null;
            }

            string homeEnvVar = Environment.GetEnvironmentVariable("HOME");
            if (String.IsNullOrEmpty(homeEnvVar))
            {
                return null;
            }

            // TODO: Remove BETA moniker from below.
            string fullPathToKeys = Path.Combine(homeEnvVar, "ASP.NET", "keys-BETA");
            return new DirectoryInfo(fullPathToKeys);
        }

        private static DirectoryInfo TryGetLocalAppDataKeysFolderForUser()
        {
#if !ASPNETCORE50
            // Environment.GetFolderPath returns null if the user profile isn't loaded.
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (folderPath != null)
            {
                // TODO: Remove BETA moniker from below.
                return new DirectoryInfo(Path.Combine(folderPath, "ASP.NET", "keys-BETA"));
            }
            else
            {
                return null;
            }
#else
            // On core CLR, we need to fall back to environment variables.
            string folderPath = Environment.GetEnvironmentVariable("LOCALAPPDATA")
                ?? Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), "AppData", "Local");

            // TODO: Remove BETA moniker from below.
            DirectoryInfo retVal = new DirectoryInfo(Path.Combine(folderPath, "ASP.NET", "keys-BETA"));
            try
            {
                retVal.Create(); // throws if we don't have access, e.g., user profile not loaded
                return retVal;
            } catch
            {
                return null;
            }
#endif
        }
    }
}
