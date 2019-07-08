﻿using StackExchange.Redis;

namespace iBlogs.Site.Core.Common.Caching
{
    /// <inheritdoc cref="IRedisConnectionWrapper"/>
    public class RedisConnectionWrapper : IRedisConnectionWrapper
    {
        private readonly RedisConnectionOption _option;
        private volatile ConnectionMultiplexer _connection;
        private readonly object _lock = new object();

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="option"></param>
        public RedisConnectionWrapper(RedisConnectionOption option)
        {
            _option = option;
        }

        /// <summary>
        /// 双检锁控制唯一可用连接
        /// </summary>
        /// <returns></returns>
        protected ConnectionMultiplexer GetConnection()
        {
            if (_connection != null && _connection.IsConnected) return _connection;

            lock (_lock)
            {
                if (_connection != null && _connection.IsConnected) return _connection;

                _connection?.Dispose();

                _connection = ConnectionMultiplexer.Connect(_option.ConnectionString);
            }

            return _connection;
        }

        /// <inheritdoc cref="IRedisConnectionWrapper"/>
        public IDatabase GetDatabase(int? db = null)
        {
            return GetConnection().GetDatabase(db ?? -1);
        }

        /// <inheritdoc cref="IRedisConnectionWrapper"/>
        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
