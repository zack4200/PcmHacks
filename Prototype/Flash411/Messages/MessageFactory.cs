﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flash411
{
    /// <summary>
    /// This class is responsible for generating the messages that the app sends to the PCM.
    /// </summary>
    /// <remarks>
    /// The messages generated by this class are byte-for-byte exactly what the PCM 
    /// receives, with the exception of the CRC byte at the end. CRC bytes must be 
    /// added by the currently-selected Device class if the actual device doesn't add 
    /// the CRC byte automatically.
    ///
    /// Some devices will require these messages to be translated according to the specific
    /// device's protocol - that too is the job of the currently-selected Device class.
    /// </remarks>
    class MessageFactory
    {
        /// <summary>
        /// Create a request to read the given block of PCM memory.
        /// </summary>
        public Message CreateReadRequest(byte Block)
        { 
            byte[] Bytes = new byte[] { Priority.Type2, DeviceId.Pcm, DeviceId.Tool, Mode.ReadBlock, Block };
            return new Message(Bytes);
        }

        /// <summary>
        /// Create a request to read the PCM's operating system ID.
        /// </summary>
        /// <returns></returns>
        public Message CreateOperatingSystemIdReadRequest()
        {
            return CreateReadRequest(BlockId.OperatingSystemID);
        }

        /// <summary>
        /// Create a request to read the PCM's Calibration ID.
        /// </summary>
        /// <returns></returns>
        public Message CreateCalibrationIdReadRequest()
        {
            return CreateReadRequest(BlockId.CalibrationID);
        }

        /// <summary>
        /// Create a request to read the PCM's Hardware ID.
        /// </summary>
        /// <returns></returns>
        public Message CreateHardwareIdReadRequest()
        {
            return CreateReadRequest(BlockId.HardwareID);
        }

        /// <summary>
        /// Create a request to read the first segment of the PCM's VIN.
        /// </summary>
        public Message CreateVinRequest1()
        {
            return CreateReadRequest(BlockId.Vin1);
        }

        /// <summary>
        /// Create a request to read the second segment of the PCM's VIN.
        /// </summary>
        public Message CreateVinRequest2()
        {
            return CreateReadRequest(BlockId.Vin2);
        }

        /// <summary>
        /// Create a request to read the thid segment of the PCM's VIN.
        /// </summary>
        public Message CreateVinRequest3()
        {
            return CreateReadRequest(BlockId.Vin3);
        }

        /// <summary>
        /// Create a request to read the first segment of the PCM's Serial Number.
        /// </summary>
        public Message CreateSerialRequest1()
        {
            return CreateReadRequest(BlockId.Serial1);
        }

        /// <summary>
        /// Create a request to read the second segment of the PCM's Serial Number.
        /// </summary>
        public Message CreateSerialRequest2()
        {
            return CreateReadRequest(BlockId.Serial2);
        }

        /// <summary>
        /// Create a request to read the thid segment of the PCM's Serial Number.
        /// </summary>
        public Message CreateSerialRequest3()
        {
            return CreateReadRequest(BlockId.Serial3);
        }

        /// <summary>
        /// Create a request to read the Broad Cast Code (BCC).
        /// </summary>
        public Message CreateBCCRequest()
        {
            return CreateReadRequest(BlockId.BCC);
        }

        /// <summary>
        /// Create a request to read the Broad Cast Code (MEC).
        /// </summary>
        public Message CreateMECRequest()
        {
            return CreateReadRequest(BlockId.MEC);
        }

        /// <summary>
        /// Create a request to retrieve a 'seed' value from the PCM
        /// </summary>
        public Message CreateSeedRequest()
        {
            byte[] Bytes = new byte[] { Priority.Type2, DeviceId.Pcm, DeviceId.Tool, Mode.Seed, SubMode.GetSeed };
            return new Message(Bytes);
        }

        /// <summary>
        /// Create a request to send a 'key' value to the PCM
        /// </summary>
        public Message CreateUnlockRequest(UInt16 Key)
        {
            byte KeyHigh = (byte)((Key & 0xFF00) >> 8);
            byte KeyLow = (byte)(Key & 0xFF);
            byte[] Bytes = new byte[] { Priority.Type2, DeviceId.Pcm, DeviceId.Tool, Mode.Seed, SubMode.SendKey, KeyHigh, KeyLow };
            return new Message(Bytes);
        }

        /// <summary>
        /// Create a block message from the supplied arguments.
        /// </summary>
        public Message CreateBlockMessage(byte[] Payload, int Offset, int Length, int Address, bool Execute)
        {
            byte[] Buffer = new byte[10 + Length + 2];
            byte[] Header = new byte[10];

            byte Exec = SubMode.NoExecute;
            if (Execute == true) Exec = SubMode.Execute;

            byte Size1 = unchecked((byte)(Length >> 8));
            byte Size2 = unchecked((byte)(Length & 0xFF));
            byte Addr1 = unchecked((byte)(Address >> 16));
            byte Addr2 = unchecked((byte)(Address >> 8));
            byte Addr3 = unchecked((byte)(Address & 0xFF));

            Header[0] = Priority.Block;
            Header[1] = DeviceId.Pcm;
            Header[2] = DeviceId.Tool;
            Header[3] = Mode.PCMUpload;
            Header[4] = Exec;
            Header[5] = Size1;
            Header[6] = Size2;
            Header[7] = Addr1;
            Header[8] = Addr2;
            Header[9] = Addr3;

            System.Buffer.BlockCopy(Header, 0, Buffer, 0, Header.Length);
            System.Buffer.BlockCopy(Payload, Offset, Buffer, Header.Length, Length);

            return new Message(AddBlockChecksum(Buffer));
        }

        /// <summary>
        /// Write a 16 bit sum to the end of a block, returns a Message, as a byte array
        /// </summary>
        /// <remarks>
        /// Overwrites the last 2 bytes at the end of the array with the sum
        /// </remarks>
        public byte[] AddBlockChecksum(byte[] Block)
        {
            UInt16 Sum = 0;

            for (int i = 4; i < Block.Length-2; i++) // skip prio, dest, src, mode
            {
                Sum += Block[i];
            }

            Block[Block.Length - 2] = unchecked((byte)(Sum >> 8));
            Block[Block.Length - 1] = unchecked((byte)(Sum & 0xFF));

            return Block;
        }

        /// <summary>
        /// Create a request for the PCM to test VPW speed switch to 4x is OK
        /// </summary>
        public Message CreateHighSpeedCheck()
        {
            return new Message(new byte[] { Priority.Type2, DeviceId.Broadcast, DeviceId.Tool, Mode.HighSpeedPrepare});
        }

        /// <summary>
        /// PCM Response if a switch to VPW 4x is OK
        /// </summary>
        public Message CreateHighSpeedOKResponse()
        {
            return new Message(new byte[] { Priority.Type2, DeviceId.Tool, DeviceId.Broadcast, Mode.HighSpeedPrepare + Mode.Response });
        }


        /// <summary>
        /// Create a request for the PCM to switch to VPW 4x
        /// </summary>
        public Message CreateBeginHighSpeed()
        {
            return new Message(new byte[] { Priority.Type2, DeviceId.Broadcast, DeviceId.Tool, Mode.HighSpeed });
        }

        /// <summary>
        /// Create a broadcast message announcing there is a test device connected to the vehicle
        /// </summary>
        public Message CreateTestDevicePresent()
        {
            byte[] bytes = new byte[] { Priority.Type2, DeviceId.Broadcast, DeviceId.Tool, Mode.TestDevicePresent };
            return new Message(bytes);
        }

        /// <summary>
        /// Create a broadcast message telling the PCM to clear DTCs
        /// </summary>
        public Message CreateClearDTCs()
        {
            byte[] bytes = new byte[] { Priority.Type0, 0x6A, DeviceId.Tool, Mode.ClearDTCs };
            return new Message(bytes);
        }

        /// <summary>
        /// A successfull response seen after the Clear DTCs message
        /// </summary>
        public Message CreateClearDTCsOK()
        {
            byte[] bytes = new byte[] { 0x48, 0x6B, DeviceId.Pcm, Mode.ClearDTCs + Mode.Response };
            return new Message(bytes);
        }

        /// <summary>
        /// Create a broadcast message telling all devices to disable normal message transmission (disable chatter)
        /// </summary>
        public Message CreateDisableNormalMessageTransmition()
        {
            byte[] Bytes = new byte[] { Priority.Type2, DeviceId.Broadcast, DeviceId.Tool, Mode.SilenceBus, SubMode.Null };
            return new Message(Bytes);
        }

        /// <summary>
        /// Create a broadcast message telling all devices to disable normal message transmission (disable chatter)
        /// </summary>
        public Message CreateDisableNormalMessageTransmitionOK()
        {
            byte[] bytes = new byte[] { Priority.Type2, DeviceId.Tool, DeviceId.Pcm, Mode.SilenceBus + Mode.Response , SubMode.Null };
            return new Message(bytes);
        }

        /// <summary>
        /// Create a request to uploade size bytes to the given address
        /// </summary>
        /// <remarks>
        /// Note that mode 0x34 is only a request. The actual payload is sent as a mode 0x36.
        /// </remarks>
        public Message CreateUploadRequest(int Size, int Address)
        {
            byte[] requestupload = { Priority.Type2, DeviceId.Pcm, DeviceId.Tool, Mode.PCMUploadRequest, SubMode.Null, 0x00, 0x00, 0x00, 0x00, 0x00 };
            requestupload[5] = unchecked((byte)(Size >> 8));
            requestupload[6] = unchecked((byte)(Size & 0xFF));
            requestupload[7] = unchecked((byte)(Address >> 16));
            requestupload[8] = unchecked((byte)(Address >> 8));
            requestupload[9] = unchecked((byte)(Address & 0xFF));
            
            return new Message(requestupload);
        }

        /// <summary>
        /// This is the successessfull response signalling an upload is allowed
        /// </summary>
        public Message CreateUploadRequestOK()
        {
            byte[] RequestAccepted = { Priority.Type2, DeviceId.Tool, DeviceId.Pcm, Mode.PCMUpload + Mode.Response, SubMode.UploadOK };
            return new Message(RequestAccepted);
        }
    }
}
