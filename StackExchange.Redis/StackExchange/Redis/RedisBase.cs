﻿using System;
using System.Threading.Tasks;

namespace StackExchange.Redis
{
    internal abstract partial class RedisBase : IRedis
    {
        internal static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        internal readonly ConnectionMultiplexer multiplexer;
        protected readonly object asyncState;

        ConnectionMultiplexer IRedisAsync.Multiplexer {  get {  return multiplexer; } }
        internal RedisBase(ConnectionMultiplexer multiplexer, object asyncState)
        {
            this.multiplexer = multiplexer;
            this.asyncState = asyncState;
        }


        public virtual TimeSpan Ping(CommandFlags flags = CommandFlags.None)
        {
            var msg = ResultProcessor.TimingProcessor.CreateMessage(flags, RedisCommand.PING);
            return ExecuteSync(msg, ResultProcessor.ResponseTimer);
        }

        public virtual Task<TimeSpan> PingAsync(CommandFlags flags = CommandFlags.None)
        {
            var msg = ResultProcessor.TimingProcessor.CreateMessage(flags, RedisCommand.PING);
            return ExecuteAsync(msg, ResultProcessor.ResponseTimer);
        }

        public void Quit(CommandFlags flags = CommandFlags.None)
        {
            var msg = Message.Create(-1, flags, RedisCommand.QUIT);
            ExecuteSync(msg, ResultProcessor.DemandOK);
        }

        public Task QuitAsync(CommandFlags flags = CommandFlags.None)
        {
            var msg = Message.Create(-1, flags, RedisCommand.QUIT);
            return ExecuteAsync(msg, ResultProcessor.DemandOK);
        }

        public override string ToString()
        {
            return multiplexer.ToString();
        }

        public void Wait(Task task)
        {
            multiplexer.Wait(task);
        }
        public bool TryWait(Task task)
        {
            return task.Wait(multiplexer.TimeoutMilliseconds);
        }

        public T Wait<T>(Task<T> task)
        {
            return multiplexer.Wait(task);
        }

        public void WaitAll(params Task[] tasks)
        {
            multiplexer.WaitAll(tasks);
        }

        internal virtual Task<T> ExecuteAsync<T>(Message message, ResultProcessor<T> processor, ServerEndPoint server = null)
        {
            if (message == null) return CompletedTask<T>.Default(asyncState);
            multiplexer.CheckMessage(message);
            return multiplexer.ExecuteAsyncImpl<T>(message, processor, asyncState, server);
        }

        internal virtual T ExecuteSync<T>(Message message, ResultProcessor<T> processor, ServerEndPoint server = null)
        {
            if (message == null) return default(T); // no-op
            multiplexer.CheckMessage(message);
            return multiplexer.ExecuteSyncImpl<T>(message, processor, server);
        }

        internal virtual RedisFeatures GetFeatures(int db, RedisKey key, CommandFlags flags, out ServerEndPoint server)
        {
            server = multiplexer.SelectServer(db, RedisCommand.PING, flags, key);
            var version = server == null ? multiplexer.RawConfig.DefaultVersion : server.Version;
            return new RedisFeatures(version);
        }

        protected void WhenAlwaysOrExists(When when)
        {
            switch (when)
            {
                case When.Always:
                case When.Exists:
                    break;
                default:
                    throw new ArgumentException(when + " is not valid in this context; the permitted values are: Always, Exists");
            }
        }

        protected void WhenAlwaysOrExistsOrNotExists(When when)
        {
            switch (when)
            {
                case When.Always:
                case When.Exists:
                case When.NotExists:
                    break;
                default:
                    throw new ArgumentException(when + " is not valid in this context; the permitted values are: Always, Exists, NotExists");
            }
        }

        protected void WhenAlwaysOrNotExists(When when)
        {
            switch(when)
            {
                case When.Always:
                case When.NotExists:
                    break;
                default:
                    throw new ArgumentException(when + " is not valid in this context; the permitted values are: Always, NotExists");
            }
        }
    }
}