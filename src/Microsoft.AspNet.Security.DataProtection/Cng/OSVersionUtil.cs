﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.AspNet.Security.DataProtection.SafeHandles;

namespace Microsoft.AspNet.Security.DataProtection.Cng
{
    internal static class OSVersionUtil
    {
        private static readonly OSVersion _osVersion = GetOSVersion();

        private static OSVersion GetOSVersion()
        {
            const string BCRYPT_LIB = "bcrypt.dll";
            SafeLibraryHandle bcryptLibHandle = null;
            try
            {
                bcryptLibHandle = SafeLibraryHandle.Open(BCRYPT_LIB);
            }
            catch
            {
                // we'll handle the exceptional case later
            }

            if (bcryptLibHandle != null)
            {
                using (bcryptLibHandle)
                {
                    if (bcryptLibHandle.DoesProcExist("BCryptKeyDerivation"))
                    {
                        // We're running on Win8+.
                        return OSVersion.Win8OrLater;
                    }
                    else
                    {
                        // We're running on Win7+.
                        return OSVersion.Win7OrLater;
                    }
                }
            }
            else
            {
                // Not running on Win7+.
                return OSVersion.NotWindows;
            }
        }

        public static bool IsBCryptOnWin7OrLaterAvailable()
        {
            return (_osVersion >= OSVersion.Win7OrLater);
        }

        public static bool IsBCryptOnWin8OrLaterAvailable()
        {
            return (_osVersion >= OSVersion.Win8OrLater);
        }

        private enum OSVersion
        {
            NotWindows = 0,
            Win7OrLater = 1,
            Win8OrLater = 2
        }
    }
}
