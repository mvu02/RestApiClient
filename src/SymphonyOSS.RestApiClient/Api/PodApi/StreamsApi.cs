﻿// Licensed to the Symphony Software Foundation (SSF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The SSF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.

using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using SymphonyOSS.RestApiClient.Logging;

namespace SymphonyOSS.RestApiClient.Api.PodApi
{
    using System;
    using System.Collections.Generic;
    using Authentication;
    using Entities;
    using Factories;
    using Generated.OpenApi.PodApi;
    using System.Net.Http;
    using Stream = Entities.Stream;

    /// <summary>
    /// Provides methods for creating single or multi party conversations
    /// and chat rooms, by encapsulating <see cref="StreamsApi"/>,
    /// adding authentication token management and a custom execution strategy.
    /// </summary>
    public class StreamsApi
    {
        private readonly Generated.OpenApi.PodApi.StreamsClient _streamsApi;
        private readonly Generated.OpenApi.PodApi.ImClient _imApi;
        private readonly Generated.OpenApi.PodApi.RoomClient _roomApi;

        private readonly IAuthTokens _authTokens;

        private readonly IApiExecutor _apiExecutor;

        private readonly ILogger _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamsApi" /> class.
        /// See <see cref="Factories.PodApiFactory"/> for conveniently constructing
        /// an instance.
        /// </summary>
        /// <param name="authTokens">Authentication tokens.</param>
        /// <param name="configuration">Api configuration.</param>
        /// <param name="apiExecutor">Execution strategy.</param>
        public StreamsApi(IAuthTokens authTokens, string baseUrl, HttpClient httpClient, IApiExecutor apiExecutor)
        {
            _streamsApi = new Generated.OpenApi.PodApi.StreamsClient(baseUrl, httpClient);
            _imApi = new Generated.OpenApi.PodApi.ImClient(baseUrl, httpClient);
            _roomApi = new RoomClient(baseUrl, httpClient);

            _authTokens = authTokens;
            _apiExecutor = apiExecutor;

            _log = ApiLogging.LoggerFactory?.CreateLogger<StreamsApi>();
        }

        /// <summary>
        /// Create a new single or multi party instant message conversation. At least one user
        /// ID must be provided or an error response will be sent. The caller is implicitly
        /// included in the members of the created chat. Duplicate users will be included in
        /// the membership of the chat but the duplication will be silently ignored.
        /// If there is an existing IM conversation with the same set of participants then
        /// the id of that existing stream will be returned.
        /// </summary>
        /// <param name="userIdList">List of User IDs of participants.</param>
        /// <returns>The ID of the created stream.</returns>
        public string CreateStream(List<long> userIdList)
        {
            try
            {
                var stream = _apiExecutor.Execute(_imApi.V1CreateAsync, userIdList, _authTokens.SessionToken);
                return stream.Id;
            }
            catch (Exception e)
            {
                _log?.LogError(0, e, "An error has occured while trying to create a stream.");
                throw;
            }
        }

        /// <summary>
        /// Get information about a particular stream.
        /// </summary>
        /// <param name="sid">Stream ID.</param>
        /// <returns>The stream's attributes.</returns>
        public Stream GetStreamInfo(string sid)
        {
            try
            {
                var streamAttributes = _apiExecutor.Execute(_streamsApi.V1InfoAsync, sid, _authTokens.SessionToken);
                return StreamFactory.Create(streamAttributes);
            }
            catch (Exception e)
            {
                _log?.LogError(0, e, "An error has occured while trying to get information about stream {sid}.", sid);
                throw;
            }
        }

        /// <summary>
        /// Create a new chatroom. If no attributes are specified, the room is created as a
        /// private chatroom.
        /// </summary>
        /// <param name="room">Room attributes.</param>
        /// <returns>The created room.</returns>
        public Room CreateRoom(Room room)
        {
            var keywords = new ObservableCollection<RoomTag>();
            foreach (var keyword in room.Keywords)
            {
                keywords.Add(new RoomTag() {Key = keyword.Item1, Value = keyword.Item2});
            }
            var v2RoomAttributes = new V2RoomAttributes()
            {
                Name = room.Name,
                Keywords = keywords,
                Description = room.Description,
                MembersCanInvite = room.MembersCanInvite,
                Discoverable = room.Discoverable,
                Public = room.Public,
                ReadOnly = room.ReadOnly,
                CopyProtected = room.CopyProtected
            };

            try
            {
                var v2RoomDetail = _apiExecutor.Execute(_roomApi.V2CreateAsync, v2RoomAttributes, _authTokens.SessionToken);
                return RoomFactory.Create(v2RoomDetail);
            }
            catch (Exception e)
            {
                _log?.LogError(0, e, "An error has occured while trying to create a room.");
                throw;
            }
            //var v2RoomDetail = _apiExecutor.Execute(_streamsApi.V2RoomCreatePost, v2RoomAttributes, _authTokens.SessionToken);
        }

        /// <summary>
        /// Get information about a chatrooom.
        /// </summary>
        /// <param name="id">The room ID.</param>
        /// <returns>The room details.</returns>
        public Room GetRoomInfo(string id)
        {
            try
            {
                var v2RoomDetail = _apiExecutor.Execute(_roomApi.V2InfoAsync, id, _authTokens.SessionToken);
                return RoomFactory.Create(v2RoomDetail);
            }
            catch (Exception e)
            {
                _log?.LogError(0, e, "An error has occured while trying to get information about the chatroom {id}.", id);
                throw;
            }
        }

        /// <summary>
        /// Deactivate or reactivate a chatroom. At creation, a new chatroom is active. 
        /// </summary>
        /// <param name="id">Room ID.</param>
        /// <param name="active">True if active, false if not.</param>
        /// <returns>The room.</returns>
        public RoomDetail SetRoomActive(string id, bool active)
        {
            try
            {
                return _apiExecutor.Execute(_roomApi.V1SetactiveAsync, id, active, _authTokens.SessionToken);
            }
            catch (Exception e)
            {
                _log?.LogError(0, e, "An error has occured while trying to deactivate or reactivate the chatroom {id}.", id);
                throw;
            }
        }

        /// <summary>
        /// Update the attributes of an existing chatroom. 
        /// </summary>
        /// <param name="room">The room and the attributes to update.</param>
        /// <returns>The room.</returns>
        public Room UpdateRoom(Room room)
        {
            var keywords = new ObservableCollection<RoomTag>();
            foreach (var keyword in room.Keywords)
            {
                keywords.Add(new RoomTag() { Key = keyword.Item1, Value = keyword.Item2 });
            }
            var v2RoomAttributes = new V2RoomAttributes()
            {
                Name = room.Name,
                Keywords = keywords,
                Description = room.Description,
                MembersCanInvite = room.MembersCanInvite,
                Discoverable = room.Discoverable,
                Public = room.Public,
                ReadOnly = room.ReadOnly,
                CopyProtected = room.CopyProtected
            };

            try
            {
                var v2RoomDetail = _apiExecutor.Execute(_roomApi.V2UpdateAsync, room.Id, v2RoomAttributes, _authTokens.SessionToken);
                return RoomFactory.Create(v2RoomDetail);
            }
            catch (Exception e)
            {
                _log?.LogError(0, e, "An error has occured while trying to update a chatroom.");
                throw;
            }
        }

        /// <summary>
        /// Searches for a room based on certain search criteria given.
        /// </summary>
        /// <param name="query">Search criteria.</param>
        /// <param name="skip">Number or results to skip.</param>
        /// <param name="limit">Max number of results.</param>
        /// <returns>The search results.</returns>
        public RoomSearchResults SearchRoom(RoomSearchCriteria query, int? skip = null, int? limit = null)
        {
            return _apiExecutor.Execute(_roomApi.V2SearchAsync, _authTokens.SessionToken, query, skip, limit);
        }
    }
}
