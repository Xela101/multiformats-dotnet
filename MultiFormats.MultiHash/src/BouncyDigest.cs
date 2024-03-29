﻿using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;

namespace TheDotNetLeague.MultiFormats.MultiHash
{
    /// <summary>
    ///     Thin wrapper around bouncy castle digests.
    /// </summary>
    /// <remarks>
    ///     Makes a Bouncy Caslte IDigest speak .Net HashAlgorithm.
    /// </remarks>
    internal class BouncyDigest : HashAlgorithm
    {
        private readonly IDigest digest;

        /// <summary>
        ///     Wrap the bouncy castle digest.
        /// </summary>
        public BouncyDigest(IDigest digest)
        {
            this.digest = digest;
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            digest.Reset();
        }

        /// <inheritdoc />
        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            digest.BlockUpdate(array, ibStart, cbSize);
        }

        /// <inheritdoc />
        protected override byte[] HashFinal()
        {
            var output = new byte[digest.GetDigestSize()];
            digest.DoFinal(output, 0);
            return output;
        }
    }
}