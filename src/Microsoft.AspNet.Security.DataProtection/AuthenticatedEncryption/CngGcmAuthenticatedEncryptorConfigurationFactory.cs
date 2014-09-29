﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Security.DataProtection.AuthenticatedEncryption
{
    /// <summary>
    /// A factory that is able to create a CNG-based IAuthenticatedEncryptor
    /// using CBC encryption + HMAC validation.
    /// </summary>
    public unsafe sealed class CngGcmAuthenticatedEncryptorConfigurationFactory : IAuthenticatedEncryptorConfigurationFactory
    {
        private readonly CngGcmAuthenticatedEncryptorConfigurationOptions _options;

        public CngGcmAuthenticatedEncryptorConfigurationFactory([NotNull] IOptionsAccessor<CngGcmAuthenticatedEncryptorConfigurationOptions> optionsAccessor)
        {
            _options = optionsAccessor.Options.Clone();
        }

        public IAuthenticatedEncryptorConfiguration CreateNewConfiguration()
        {
            // generate a 512-bit secret randomly
            const int KDK_SIZE_IN_BYTES = 512 / 8;
            var secret = ProtectedMemoryBlob.Random(KDK_SIZE_IN_BYTES);
            return new CngGcmAuthenticatedEncryptorConfiguration(_options, secret);
        }
    }
}
