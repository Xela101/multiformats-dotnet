using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Google.Protobuf;
using Newtonsoft.Json;
using TheDotNetLeague.MultiFormats.MultiBase;
using Xunit;

namespace TheDotNetLeague.MultiFormats.MultiHash.Tests
{
    public class MultiHashTest
    {
        [Fact]
        public void Base32_Encode()
        {
            var mh = new MultiHash("QmPZ9gcCEpqKTo6aq61g2nXGUhM4iCL3ewB6LDXZCtioEB");
            Assert.Equal("sha2-256", mh.Algorithm.Name);
            Assert.Equal(32, mh.Digest.Length);
            Assert.Equal("ciqbed3k6ya5i3qqwljochwxdrk5exzqilbckapedujenz5b5hj5r3a", mh.ToBase32());
        }

        [Fact]
        public void Base58_Encode_Decode()
        {
            var mh = new MultiHash("QmPZ9gcCEpqKTo6aq61g2nXGUhM4iCL3ewB6LDXZCtioEB");
            Assert.Equal("sha2-256", mh.Algorithm.Name);
            Assert.Equal(32, mh.Digest.Length);
            Assert.Equal("QmPZ9gcCEpqKTo6aq61g2nXGUhM4iCL3ewB6LDXZCtioEB", mh.ToBase58());
        }

        [Fact]
        public void Compute_Hash_Array()
        {
            var hello = Encoding.UTF8.GetBytes("Hello, world.");
            var mh = MultiHash.ComputeHash(hello);
            Assert.Equal(MultiHash.DefaultAlgorithmName, mh.Algorithm.Name);
            Assert.NotNull(mh.Digest);
        }

        [Fact]
        public void Compute_Hash_Stream()
        {
            var hello = new MemoryStream(Encoding.UTF8.GetBytes("Hello, world."));
            hello.Position = 0;
            var mh = MultiHash.ComputeHash(hello);
            Assert.Equal(MultiHash.DefaultAlgorithmName, mh.Algorithm.Name);
            Assert.NotNull(mh.Digest);
        }

        [Fact]
        public void Compute_Not_Implemented_Hash_Array()
        {
            var alg = HashingAlgorithm.Register("not-implemented", 0x0F, 32);
            try
            {
                var hello = Encoding.UTF8.GetBytes("Hello, world.");
                Assert.Throws<NotImplementedException>(() => MultiHash.ComputeHash(hello, "not-implemented"));
            }
            finally
            {
                HashingAlgorithm.Deregister(alg);
            }
        }

        [Fact]
        public void HashNames()
        {
            var mh = new MultiHash("sha1", new byte[20]);
            mh = new MultiHash("sha2-256", new byte[32]);
            mh = new MultiHash("sha2-512", new byte[64]);
            mh = new MultiHash("keccak-512", new byte[64]);
        }

        [Fact]
        public void Implicit_Conversion_From_String()
        {
            var hash = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4";
            MultiHash mh = hash;
            Assert.NotNull(mh);
            Assert.IsType<MultiHash>(mh);
            Assert.Equal(hash, mh.ToString());
        }

        [Fact]
        public void Invalid_Digest()
        {
            Assert.Throws<ArgumentNullException>(() => new MultiHash("sha1", null));
            Assert.Throws<ArgumentException>(() => new MultiHash("sha1", new byte[0]));
            Assert.Throws<ArgumentException>(() => new MultiHash("sha1", new byte[21]));
        }

        [Fact]
        public void Matches_Array()
        {
            var hello = Encoding.UTF8.GetBytes("Hello, world.");
            var hello1 = Encoding.UTF8.GetBytes("Hello, world");
            var mh = MultiHash.ComputeHash(hello);
            Assert.True(mh.Matches(hello));
            Assert.False(mh.Matches(hello1));

            var mh1 = MultiHash.ComputeHash(hello, "sha1");
            Assert.True(mh1.Matches(hello));
            Assert.False(mh1.Matches(hello1));

            var mh2 = MultiHash.ComputeHash(hello, "sha2-512");
            Assert.True(mh2.Matches(hello));
            Assert.False(mh2.Matches(hello1));

            var mh3 = MultiHash.ComputeHash(hello, "keccak-512");
            Assert.True(mh3.Matches(hello));
            Assert.False(mh3.Matches(hello1));
        }

        [Fact]
        public void Matches_Stream()
        {
            var hello = new MemoryStream(Encoding.UTF8.GetBytes("Hello, world."));
            var hello1 = new MemoryStream(Encoding.UTF8.GetBytes("Hello, world"));
            hello.Position = 0;
            var mh = MultiHash.ComputeHash(hello);

            hello.Position = 0;
            Assert.True(mh.Matches(hello));

            hello1.Position = 0;
            Assert.False(mh.Matches(hello1));
        }

        [Fact]
        public void Parsing_Unknown_Hash_Number()
        {
            HashingAlgorithm unknown = null;
            EventHandler<UnknownHashingAlgorithmEventArgs> unknownHandler = (s, e) => { unknown = e.Algorithm; };
            var ms = new MemoryStream(new byte[] {0x01, 0x02, 0x0a, 0x0b});
            MultiHash.UnknownHashingAlgorithm += unknownHandler;
            try
            {
                var mh = new MultiHash(ms);
                Assert.Equal("ipfs-1", mh.Algorithm.Name);
                Assert.Equal("ipfs-1", mh.Algorithm.ToString());
                Assert.Equal(1, mh.Algorithm.Code);
                Assert.Equal(2, mh.Algorithm.DigestSize);
                Assert.Equal(0xa, mh.Digest[0]);
                Assert.Equal(0xb, mh.Digest[1]);
                Assert.NotNull(unknown);
                Assert.Equal("ipfs-1", unknown.Name);
                Assert.Equal(1, unknown.Code);
                Assert.Equal(2, unknown.DigestSize);
            }
            finally
            {
                MultiHash.UnknownHashingAlgorithm -= unknownHandler;
            }
        }

        [Fact]
        public void Parsing_Wrong_Digest_Size()
        {
            var ms = new MemoryStream(new byte[] {0x11, 0x02, 0x0a, 0x0b});
            Assert.Throws<InvalidDataException>(() => new MultiHash(ms));
        }

        [Fact]
        public void To_String_Is_Base58_Representation()
        {
            var hash = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4";
            var mh = new MultiHash(hash);
            Assert.Equal(hash, mh.ToString());
        }

        [Fact]
        public void Unknown_Hash_Name()
        {
            Assert.Throws<ArgumentNullException>(() => new MultiHash(null, new byte[0]));
            Assert.Throws<ArgumentException>(() => new MultiHash("", new byte[0]));
            Assert.Throws<ArgumentException>(() => new MultiHash("md5", new byte[0]));
        }

        [Fact]
        public void Value_Equality()
        {
            var a0 = new MultiHash("QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4");
            var a1 = new MultiHash("QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4");
            var b = new MultiHash("QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L5");
            MultiHash c = null;
            MultiHash d = null;

            Assert.True(c == d);
            Assert.False(c == b);
            Assert.False(b == c);

            Assert.False(c != d);
            Assert.True(c != b);
            Assert.True(b != c);

#pragma warning disable 1718
            Assert.True(a0 == a0);
            Assert.True(a0 == a1);
            Assert.False(a0 == b);

#pragma warning disable 1718
            Assert.False(a0 != a0);
            Assert.False(a0 != a1);
            Assert.True(a0 != b);

            Assert.True(a0.Equals(a0));
            Assert.True(a0.Equals(a1));
            Assert.False(a0.Equals(b));

            Assert.Equal(a0, a0);
            Assert.Equal(a0, a1);
            Assert.NotEqual(a0, b);

            Assert.Equal(a0, a0);
            Assert.Equal(a0, a1);
            Assert.NotEqual(a0, b);

            Assert.Equal(a0.GetHashCode(), a0.GetHashCode());
            Assert.Equal(a0.GetHashCode(), a1.GetHashCode());
            Assert.NotEqual(a0.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Wire_Formats()
        {
            var hashes = new[]
            {
                "5drNu81uhrFLRiS4bxWgAkpydaLUPW", // sha1
                "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4", // sha2_256
                "8Vtkv2tdQ43bNGdWN9vNx9GVS9wrbXHk4ZW8kmucPmaYJwwedXir52kti9wJhcik4HehyqgLrQ1hBuirviLhxgRBNv" // sha2_512
            };
            var helloWorld = Encoding.UTF8.GetBytes("hello world");
            foreach (var hash in hashes)
            {
                var mh = new MultiHash(hash);
                Assert.True(mh.Matches(helloWorld), hash);
            }
        }

        [Fact]
        public void Write_Null_Stream()
        {
            var mh = new MultiHash("QmPZ9gcCEpqKTo6aq61g2nXGUhM4iCL3ewB6LDXZCtioEB");
            Assert.Throws<ArgumentNullException>(() => mh.Write((CodedOutputStream) null));
        }

        [Fact]
        public void Varint_Hash_Code_and_Length()
        {
            var concise = "1220f8c3bf62a9aa3e6fc1619c250e48abe7519373d3edf41be62eb5dc45199af2ef"
                .ToHexBuffer();
            var mh = new MultiHash(new MemoryStream(concise, false));
            Assert.Equal("sha2-256", mh.Algorithm.Name);
            Assert.Equal(0x12, mh.Algorithm.Code);
            Assert.Equal(0x20, mh.Algorithm.DigestSize);

            var longer = "9200a000f8c3bf62a9aa3e6fc1619c250e48abe7519373d3edf41be62eb5dc45199af2ef"
                .ToHexBuffer();
            mh = new MultiHash(new MemoryStream(longer, false));
            Assert.Equal("sha2-256", mh.Algorithm.Name);
            Assert.Equal(0x12, mh.Algorithm.Code);
            Assert.Equal(0x20, mh.Algorithm.DigestSize);
        }

        [Fact]
        public void Compute_Hash_All_Algorithms()
        {
            foreach (var alg in HashingAlgorithm.All)
                try
                {
                    var mh = MultiHash.ComputeHash(new byte[0], alg.Name);
                    Assert.NotNull(mh);
                    Assert.Equal(alg.Code, mh.Algorithm.Code);
                    Assert.Equal(alg.Name, mh.Algorithm.Name);
                    Assert.Equal(alg.DigestSize, mh.Algorithm.DigestSize);
                    Assert.Equal(alg.DigestSize, mh.Digest.Length);
                }
                catch (NotImplementedException)
                {
                    // If NYI then can't test it.
                }
        }

        [Fact]
        public void Example()
        {
            var hello = Encoding.UTF8.GetBytes("Hello world");
            var mh = MultiHash.ComputeHash(hello);
            Console.WriteLine($"| hash code | 0x{mh.Algorithm.Code.ToString("x")} |");
            Console.WriteLine($"| digest length | 0x{mh.Digest.Length.ToString("x")} |");
            Console.WriteLine($"| digest value | {mh.Digest.ToHexString()} |");
            Console.WriteLine($"| binary | {mh.ToArray().ToHexString()} |");
            Console.WriteLine($"| base 58 | {mh.ToBase58()} |");
            Console.WriteLine($"| base 32 | {mh.ToBase32()} |");
        }

        private class TestVector
        {
            public string Algorithm { get; set; }
            public string Input { get; set; }
            public string Output { get; set; }
            public bool Ignore { get; set; }
        }

        private readonly TestVector[] TestVectors =
        {
            // From https://github.com/multiformats/js-multihashing-async/blob/master/test/fixtures/encodes.js
            new TestVector
            {
                Algorithm = "sha1",
                Input = "beep boop",
                Output = "11147c8357577f51d4f0a8d393aa1aaafb28863d9421"
            },
            new TestVector
            {
                Algorithm = "sha2-256",
                Input = "beep boop",
                Output = "122090ea688e275d580567325032492b597bc77221c62493e76330b85ddda191ef7c"
            },
            new TestVector
            {
                Algorithm = "sha2-512",
                Input = "beep boop",
                Output =
                    "134014f301f31be243f34c5668937883771fa381002f1aaa5f31b3f78e500b66ff2f4f8ea5e3c9f5a61bd073e2452c480484b02e030fb239315a2577f7ae156af177"
            },
            new TestVector
            {
                Algorithm = "sha3-512",
                Input = "beep boop",
                Output =
                    "1440fae2c9eb19906057f8bf507f0e73ee02bb669d58c3069e7718b89ca4d314cf4fd6f1679019cc46d185c7af34f6c05a307b070e74e9ed5b9c64f86aacc2b90d10"
            },
            new TestVector
            {
                Algorithm = "sha3-384",
                Input = "beep boop",
                Output =
                    "153075a9cff1bcfbe8a7025aa225dd558fb002769d4bf3b67d2aaf180459172208bea989804aefccf060b583e629e5f41e8d"
            },
            new TestVector
            {
                Algorithm = "sha3-256",
                Input = "beep boop",
                Output = "1620828705da60284b39de02e3599d1f39e6c1df001f5dbf63c9ec2d2c91a95a427f"
            },
            new TestVector
            {
                Algorithm = "sha3-224",
                Input = "beep boop",
                Output = "171c0da73a89549018df311c0a63250e008f7be357f93ba4e582aaea32b8"
            },
            new TestVector
            {
                Algorithm = "shake-128",
                Input = "beep boop",
                Output = "18105fe422311f770743c2e0d86bcca09211"
            },
            new TestVector
            {
                Algorithm = "shake-256",
                Input = "beep boop",
                Output = "192059feb5565e4f924baef74708649fed376d63948a862322ed763ecf093b63b38b"
            },
            new TestVector
            {
                Algorithm = "keccak-224",
                Input = "beep boop",
                Output = "1a1c2bd72cde2f75e523512999eb7639f17b699efe29bec342f5a0270896"
            },
            new TestVector
            {
                Algorithm = "keccak-256",
                Input = "beep boop",
                Output = "1b20ee6f6b4ce5d754c01203cb3b99294b88615df5999e20d6fe509204fa254a0f97"
            },
            new TestVector
            {
                Algorithm = "keccak-384",
                Input = "beep boop",
                Output =
                    "1c300e2fcca40e861fc425a2503a65f4a4befab7be7f193e57654ca3713e85262b035e54d5ade93f9632b810ab88b04f7d84"
            },
            new TestVector
            {
                Algorithm = "keccak-512",
                Input = "beep boop",
                Output =
                    "1d40e161c54798f78eba3404ac5e7e12d27555b7b810e7fd0db3f25ffa0c785c438331b0fbb6156215f69edf403c642e5280f4521da9bd767296ec81f05100852e78"
            },
            new TestVector
            {
                Algorithm = "blake2b-512",
                Input = "beep boop",
                Output =
                    "c0e402400eac6255ba822373a0948122b8d295008419a8ab27842ee0d70eca39855621463c03ec75ac3610aacfdff89fa989d8d61fc00450148f289eb5b12ad1a954f659"
            },
            new TestVector
            {
                Algorithm = "blake2b-160",
                Input = "beep boop",
                Output = "94e40214fe303247293e54e0a7ea48f9408ca68b36b08442"
            },
            new TestVector
            {
                Algorithm = "blake2s-256",
                Input = "beep boop",
                Output = "e0e402204542eaca484e4311def8af74b546edd7fceb49eeb3cdcfd8a4a72ed0dc81d4c0"
            },
            new TestVector
            {
                Algorithm = "dbl-sha2-256",
                Input = "beep boop",
                Output = "56209cd9115d76945c2455b1450295b05f4edeba2e7286bc24c23e266b48faf578c0"
            },
            new TestVector
            {
                Algorithm = "identity",
                Input = "ab",
                Output = "00026162"
            },
            new TestVector
            {
                Algorithm = "id",
                Input = "ab",
                Output = "00026162"
            }
        };

        [Fact]
        public void CheckMultiHash()
        {
            foreach (var v in TestVectors)
            {
                if (v.Ignore) continue;
                var bytes = Encoding.UTF8.GetBytes(v.Input);
                var mh = MultiHash.ComputeHash(bytes, v.Algorithm);
                Assert.Equal(v.Output, mh.ToArray().ToHexString());
            }
        }

        [Fact]
        public void CheckMultiHash_Stream()
        {
            foreach (var v in TestVectors)
            {
                if (v.Ignore) continue;
                var bytes = Encoding.UTF8.GetBytes(v.Input);
                using (var ms = new MemoryStream(bytes, false))
                {
                    var mh = MultiHash.ComputeHash(ms, v.Algorithm);
                    Assert.Equal(v.Output, mh.ToArray().ToHexString());
                }
            }
        }

        [Fact]
        public void IdentityHash()
        {
            var hello = Encoding.UTF8.GetBytes("Hello, world.");
            var mh = MultiHash.ComputeHash(hello);
            Assert.False(mh.IsIdentityHash);
            hello.SequenceEqual(mh.Digest).Should().BeFalse();

            mh = MultiHash.ComputeHash(hello, "identity");
            Assert.True(mh.IsIdentityHash);
            hello.Should().Contain(mh.Digest);

            var mh1 = new MultiHash(mh.ToBase58());
            Assert.Equal(mh, mh1);

            mh = MultiHash.ComputeHash(hello, "id");
            Assert.True(mh.IsIdentityHash);
            hello.Should().Contain(mh.Digest);

            mh1 = new MultiHash(mh.ToBase58());
            Assert.Equal(mh, mh1);
        }

        [Fact]
        public void Binary()
        {
            var mh = new MultiHash("QmPZ9gcCEpqKTo6aq61g2nXGUhM4iCL3ewB6LDXZCtioEB");
            Assert.Equal("sha2-256", mh.Algorithm.Name);
            Assert.Equal(32, mh.Digest.Length);

            var binary = mh.ToArray();
            var mh1 = new MultiHash(binary);
            Assert.Equal(mh.Algorithm.Name, mh1.Algorithm.Name);
            mh.Digest.Should().Contain(mh1.Digest);
        }

        [Fact]
        public void JsonSerialization()
        {
            var a = new MultiHash("QmPZ9gcCEpqKTo6aq61g2nXGUhM4iCL3ewB6LDXZCtioEB");
            var json = JsonConvert.SerializeObject(a);
            Assert.Equal($"\"{a}\"", json);
            var b = JsonConvert.DeserializeObject<MultiHash>(json);
            Assert.Equal(a, b);

            a = null;
            json = JsonConvert.SerializeObject(a);
            b = JsonConvert.DeserializeObject<MultiHash>(json);
            Assert.Null(b);
        }

        [Fact]
        public void CodeToName()
        {
            Assert.Equal("sha2-512", MultiHash.GetHashAlgorithmName(0x13));
            Assert.Throws<KeyNotFoundException>(
                () => MultiHash.GetHashAlgorithmName(0xbadbad));
        }

        [Fact]
        public void GetAlgorithmByName()
        {
            Assert.NotNull(MultiHash.GetHashAlgorithm());
            Assert.NotNull(MultiHash.GetHashAlgorithm("sha2-512"));
            var e = Assert.Throws<KeyNotFoundException>(() =>
            {
                var _ = MultiHash.GetHashAlgorithm("unknown");
            });
        }
    }
}