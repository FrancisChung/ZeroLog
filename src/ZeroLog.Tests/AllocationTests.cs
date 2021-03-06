﻿using System;
using System.IO;
using System.Text.Formatting;
using System.Threading;
using NCrunch.Framework;
using NFluent;
using NUnit.Framework;
using ZeroLog.Appenders;
using ZeroLog.Config;

namespace ZeroLog.Tests
{
    [TestFixture]
    public class AllocationTests
    {
        private WaitableAppender _waitableAppender;
        private string _tempDirectory;

        public class WaitableAppender : DateAndSizeRollingFileAppender
        {
            public int WrittenEventCount { get; private set; }

            public WaitableAppender(string filePathRoot)
                : base(filePathRoot)
            {
            }

            public override void WriteEvent(ILogEventHeader logEventHeader, byte[] messageBytes, int messageLength)
            {
                WrittenEventCount++;
                base.WriteEvent(logEventHeader, messageBytes, messageLength);
            }
        }

        [SetUp]
        public void Setup()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);

            _waitableAppender = new WaitableAppender(Path.Combine(_tempDirectory, "allocation-test"));

            BasicConfigurator.Configure(new ZeroLogBasicConfiguration
            {
                Appenders = { _waitableAppender },
                LogEventQueueSize = 2048 * 10,
                LogEventBufferSize = 512
            });

            LogManager.RegisterEnum<DayOfWeek>();
            LogManager.RegisterUnmanaged<UnmanagedStruct>();
        }

        [TearDown]
        public void Teardown()
        {
            LogManager.Shutdown();
            Directory.Delete(_tempDirectory, true);
        }

        [Test]
        public void should_not_allocate_using_all_formats_and_file_appender()
        {
            // Allocation tests are unreliable when run from NCrunch
            if (NCrunchEnvironment.NCrunchIsResident())
                Assert.Inconclusive();

            var log = LogManager.GetLogger("AllocationTest");

            GC.Collect(2, GCCollectionMode.Forced, true);
            var gcCountBefore = GC.CollectionCount(0);

            var numberOfEvents = 2048 * 10;

            for (var i = 0; i < numberOfEvents; i++)
            {
                log.InfoFormat(
                    "Int {0}, Double {1:N4}, String {2}, Bool {3}, Decimal {4:N4}, Guid {5}, Timestamp {6}, DateTime {7}",
                    123243,
                    32423432.4398438,
                    "Some random string",
                    true,
                    4234324324.23423423,
                    Guid.NewGuid(),
                    DateTime.UtcNow.TimeOfDay,
                    DateTime.UtcNow
                );

                log.InfoFormat(
                    "Enum {0}, UnknownEnum {1}, NullableEnum {2}, NullableNullEnum {3}, NullableInt {4}, NullableNullInt {5}",
                    DayOfWeek.Friday,
                    UnregisteredEnum.Bar,
                    (DayOfWeek?)DayOfWeek.Monday,
                    (DayOfWeek?)null,
                    (int?)42,
                    (int?)null
                );
            }

            // Give the appender some time to finish writing to file
            while (_waitableAppender.WrittenEventCount < numberOfEvents)
                Thread.Sleep(1);

            var gcCountAfter = GC.CollectionCount(0);

            Check.That(gcCountBefore).IsEqualTo(gcCountAfter);
        }

        [Test]
        public void should_not_allocate_using_all_formats_and_file_appender_builder()
        {
            // Allocation tests are unreliable when run from NCrunch
            if (NCrunchEnvironment.NCrunchIsResident())
                Assert.Inconclusive();

            var log = LogManager.GetLogger("AllocationTest");

            GC.Collect(2, GCCollectionMode.Forced, true);
            var gcCountBefore = GC.CollectionCount(0);

            var numberOfEvents = 2048 * 10;

            for (var i = 0; i < numberOfEvents; i++)
            {
                log
                    .Info()
                    .Append("Int ")
                    .Append(123243)
                    .Append("Double ")
                    .Append(32423432.4398438, "N4")
                    .Append("String ")
                    .Append("Some random string")
                    .Append("Bool ")
                    .Append(true)
                    .Append("Decimal ")
                    .Append(4234324324.23423423, "N4")
                    .Append("Guid ")
                    .Append(Guid.NewGuid())
                    .Append("Timestamp ")
                    .Append(DateTime.UtcNow.TimeOfDay)
                    .Append("DateTime ")
                    .Append(DateTime.UtcNow)
                    .Log();

                log
                    .Info()
                    .Append("Enum ")
                    .AppendEnum(DayOfWeek.Friday)
                    .Append("UnknownEnum ")
                    .AppendEnum(UnregisteredEnum.Bar)
                    .Append("NullableEnum ")
                    .AppendEnum((DayOfWeek?)DayOfWeek.Monday)
                    .Append("NullableNullEnum ")
                    .AppendEnum((DayOfWeek?)null)
                    .Append("NullableInt ")
                    .Append((int?)42)
                    .Append("NullableNullInt ")
                    .Append((int?)null)
                    .Log();

                var unmanaged = new UnmanagedStruct(1, 2, 3);
                var unregistered_unmanaged = new UnregisteredUnmanagedStruct(4, 5, 6);
                UnmanagedStruct? nullable_unmanaged = unmanaged;
                UnmanagedStruct? null_nullable_unmanaged = (UnmanagedStruct?)null;
                UnregisteredUnmanagedStruct? nullable_unregistered_unmanaged = new UnregisteredUnmanagedStruct(4, 5, 6);
                UnregisteredUnmanagedStruct? null_nullable_unregistered_unmanaged = (UnregisteredUnmanagedStruct?)null;

                log
                    .Info()
                    .Append("Unmanaged Struct ")
                    .AppendUnmanaged(unmanaged)
                    .Append("Unregistered Unmanaged Struct ")
                    .AppendUnmanaged(unregistered_unmanaged)

                    .Append("Unmanaged Struct byref ")
                    .AppendUnmanaged(ref unmanaged)
                    .Append("Unregistered Unmanaged byref ")
                    .AppendUnmanaged(ref unregistered_unmanaged)

                    .Append("Nullable Unmanaged ")
                    .AppendUnmanaged(nullable_unmanaged)
                    .Append("Null Nullable Unmanaged ")
                    .AppendUnmanaged(null_nullable_unmanaged)
                    .Append("Nullable Unregistered Unmanaged ")
                    .AppendUnmanaged(nullable_unregistered_unmanaged)
                    .Append("Null Nullable Unregistered Unmanaged")
                    .AppendUnmanaged(null_nullable_unregistered_unmanaged)

                    .Append("Nullable Unmanaged byref ")
                    .AppendUnmanaged(ref nullable_unmanaged)
                    .Append("Null Nullable Unmanaged byref ")
                    .AppendUnmanaged(ref null_nullable_unmanaged)
                    .Append("Nullable Unregistered Unmanaged byref ")
                    .AppendUnmanaged(ref nullable_unregistered_unmanaged)
                    .Append("Null Nullable Unregistered Unmanaged byref ")
                    .AppendUnmanaged(ref null_nullable_unregistered_unmanaged)

                    .Log();
            }

            // Give the appender some time to finish writing to file
            while (_waitableAppender.WrittenEventCount < numberOfEvents)
                Thread.Sleep(1);

            var gcCountAfter = GC.CollectionCount(0);

            Check.That(gcCountBefore).IsEqualTo(gcCountAfter);
        }

        private enum UnregisteredEnum
        {
            Foo,
            Bar,
            Baz
        }

        private struct UnmanagedStruct : IStringFormattable
        {
            public long A;
            public int B;
            public byte C;

            public UnmanagedStruct(long a, int b, byte c)
            {
                this.A = a;
                this.B = b;
                this.C = c;
            }

            public void Format(StringBuffer buffer, StringView format)
            {
                buffer.Append(this.A, StringView.Empty);
                buffer.Append("-");
                buffer.Append(this.B, StringView.Empty);
                buffer.Append("-");
                buffer.Append(this.C, StringView.Empty);
            }
        }

        private struct UnregisteredUnmanagedStruct : IStringFormattable
        {
            public long D;
            public int E;
            public byte F;

            public UnregisteredUnmanagedStruct(long d, int e, byte f)
            {
                this.D = d;
                this.E = e;
                this.F = f;
            }

            public void Format(StringBuffer buffer, StringView format)
            {
                buffer.Append(this.D, StringView.Empty);
                buffer.Append("-");
                buffer.Append(this.E, StringView.Empty);
                buffer.Append("-");
                buffer.Append(this.F, StringView.Empty);
            }
        }
    }
}
