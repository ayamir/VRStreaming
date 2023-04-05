/**
 * Copyright 2021 Vasanth Mohan. All rights and licenses reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 */

using System;
using System.Collections.Generic;
using Unity.RenderStreaming;
using UnityEngine;
using Newtonsoft.Json;

namespace FusedVR.VRStreaming
{
    public class ClientStreams : MonoBehaviour
    {
        #region Variables
        /// <summary>
        /// Data Channel for the client
        /// </summary>
        [SerializeField]
        private InputChannelReceiverBase dataChannel;

        /// <summary>
        /// Streams (video, audio) that need to be sent to the client
        /// </summary>
        [SerializeField]
        private VRCamStream vrCamStreamLeft;

        [SerializeField]
        private VRCamStream vrCamStreamRight;

        [SerializeField]
        private List<AudioStreamSender> audioStreams = new List<AudioStreamSender>();

        [SerializeField]
        private bool isObjectLevelStreaming = false;

        /// <summary>
        /// Connection ID for client
        /// </summary>
        private string myConnection;

        private uint[] renderLevelSystemArray;
        private uint[] myColorsLeft;
        private uint[] myColorsRight;

        private int streamingWidth;
        private int streamingHeight;

        private const int mbWidth = 16;
        private const int mbHeight = 16;

        private int arraySize;
        #endregion

        #region Events
        /// <summary>
        /// Static events for when a client joins or leaves
        /// </summary>
        public delegate void OnClientStream(ClientStreams player);
        public static OnClientStream OnClientAdded;
        public static OnClientStream OnClientLeft;

        private void OnEnable()
        {
            OnClientAdded?.Invoke(this);
        }

        private void OnDisable()
        {
            OnClientLeft?.Invoke(this);
        }
        #endregion

        /// <summary>
        /// Set the connection based on the signalling data from the client on an Offer
        /// </summary>
        public void SetFullConnection(string connectionID, SignalingHandlerBase broadcast)
        {
            myConnection = connectionID; //save ID

            streamingWidth = vrCamStreamLeft.GetStreamingSize().x;
            streamingHeight = vrCamStreamLeft.GetStreamingSize().y;

            if (streamingWidth != vrCamStreamRight.GetStreamingSize().x)
            {
                Debug.LogError("Different camera streaming width detected");
            }
            if (streamingHeight != vrCamStreamRight.GetStreamingSize().y)
            {
                Debug.LogError("Different camera streaming height detected");
            }

            broadcast.AddSender(myConnection, vrCamStreamRight);
            broadcast.AddSender(myConnection, vrCamStreamLeft);

            foreach (AudioStreamSender audioSource in audioStreams)
            {
                broadcast.AddSender(myConnection, audioSource);
            }

            broadcast.AddChannel(myConnection, dataChannel);

            Debug.Log("FullConnection Set");
        }

        /// <summary>
        /// Enable & Disable View Streams
        /// TODO: disabling track causes the project to crash. disabling / enabling the stream seems fine
        /// Filed Issue : https://github.com/Unity-Technologies/com.unity.webrtc/issues/523
        /// </summary>
        public void ViewStreams(bool view)
        {
            vrCamStreamLeft.gameObject.SetActive(view);
            vrCamStreamRight.gameObject.SetActive(view);

            foreach (AudioStreamSender audioSource in audioStreams)
            {
                audioSource.gameObject.SetActive(view);
            }
        }

        /// <summary>
        /// Send Event Data to this Client to be processed on the data channel
        /// </summary>
        public void SendDataMessage(string evt, string data)
        {
            Dictionary<string, string> payload = new Dictionary<string, string>
            {
                { "event", evt },
                { "payload" , data}
            };
            string json = JsonConvert.SerializeObject(payload, Formatting.Indented);
            dataChannel.Channel.Send(json); //send over the data channel
        }

        /// <summary>
        /// Set the Data Channel for the Client manually
        /// </summary>
        public void SetDataChannel(SignalingEventData data)
        {
            dataChannel.SetChannel(data.connectionId, data.channel);
        }

        /// <summary>
        /// Clean up streams for a disconnected client
        /// </summary>
        public void DeleteConnection(string connectionID)
        {

            if (myConnection == connectionID)
            {
                vrCamStreamRight.SetSender(myConnection, null);
                vrCamStreamLeft.SetSender(myConnection, null);

                foreach (AudioStreamSender audioSource in audioStreams)
                {
                    audioSource.SetSender(myConnection, null);
                }

                dataChannel.SetChannel(myConnection, null);

                myConnection = null; //remove ID
            }
        }
        private void Awake()
        {
            if (isObjectLevelStreaming)
            {
                if (vrCamStreamLeft != null && vrCamStreamRight != null)
                {
                    streamingWidth = vrCamStreamLeft.GetStreamingSize().x;
                    streamingHeight = vrCamStreamLeft.GetStreamingSize().y;
                    arraySize = (streamingWidth / mbWidth) * (streamingHeight / mbHeight);
                    myColorsLeft = new uint[arraySize];
                    myColorsRight = new uint[arraySize];
                }
            }
        }

        private void LateUpdate()
        {
            if (isObjectLevelStreaming)
            {
                if (renderLevelSystemArray != null)
                {
                    Array.Copy(renderLevelSystemArray, 0, myColorsLeft, 0, arraySize);
                    SetTrackPriorityArray(vrCamStreamLeft, ref myColorsLeft);

                    Array.Copy(renderLevelSystemArray, 0, myColorsRight, 0, arraySize);
                    SetTrackPriorityArray(vrCamStreamRight, ref myColorsRight);
                }
            }
        }

        private void SetTrackPriorityArray(VRCamStream vrCamStream, ref uint[] myColors)
        {
            var track = vrCamStream.GetVideoStreamTrack();
        }
    }


}