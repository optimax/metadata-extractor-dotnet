﻿// Copyright (c) Drew Noakes and contributors. All Rights Reserved. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using MetadataExtractor.IO;

namespace MetadataExtractor.Formats.Mpeg
{
    public sealed class Mp3Reader
    {
        // http://id3.org/mp3Frame
        // https://www.loc.gov/preservation/digital/formats/fdd/fdd000105.shtml

        public Directory Extract(SequentialReader reader)
        {
            var directory = new Mp3Directory();

            var header = reader.GetInt32();

            // ID: MPEG-2.5, MPEG-2, or MPEG-1
            int id = 0;
            switch ((header & 0x000180000) >> 19)
            {
                case 0:
                    throw new ImageProcessingException("MPEG-2.5 not supported.");
                case 2:
                    directory.Set(Mp3Directory.TAG_ID, "MPEG-2");
                    id = 2;
                    break;
                case 3:
                    directory.Set(Mp3Directory.TAG_ID, "MPEG-1");
                    id = 1;
                    break;
            }

            // Layer Type: 1, 2, 3, or not defined
            int layer = (header & 0x00060000) >> 17;
            switch (layer)
            {
                case 0:
                    directory.Set(Mp3Directory.TAG_LAYER, "Not defined");
                    break;
                case 1:
                    directory.Set(Mp3Directory.TAG_LAYER, "Layer III");
                    break;
                case 2:
                    directory.Set(Mp3Directory.TAG_LAYER, "Layer II");
                    break;
                case 3:
                    directory.Set(Mp3Directory.TAG_LAYER, "Layer I");
                    break;
            }


            int protectionBit = (header & 0x00010000) >> 16;

            // Bitrate: depends on ID and Layer
            int bitrate = (header & 0x0000F000) >> 12;
            if (bitrate != 0 && bitrate != 15)
                directory.Set(Mp3Directory.TAG_BITRATE, SetBitrate(bitrate, layer, id));

            // Frequency: depends on ID
            int frequency = (header & 0x00000C00) >> 10;
            int[,] frequencyMapping = new int[2, 3]
            {
                { 44100, 48000, 32000 },
                { 22050, 24000, 16000 }
            };
            if (id == 1)
            {
                directory.Set(Mp3Directory.TAG_FREQUENCY, frequencyMapping[0, frequency]);
                frequency = frequencyMapping[0, frequency];
            }
            else if (id == 2)
            {
                directory.Set(Mp3Directory.TAG_FREQUENCY, frequencyMapping[1, frequency]);
                frequency = frequencyMapping[1, frequency];
            }


            int paddingBit = (header & 0x00000200) >> 9;

            // Encoding type: Stereo, Joint Stereo, Dual Channel, or Mono
            int mode = (header & 0x000000C0) >> 6;
            switch (mode)
            {
                case 0:
                    directory.Set(Mp3Directory.TAG_MODE, "Stereo");
                    break;
                case 1:
                    directory.Set(Mp3Directory.TAG_MODE, "Joint stereo");
                    break;
                case 2:
                    directory.Set(Mp3Directory.TAG_MODE, "Dual channel");
                    break;
                case 3:
                    directory.Set(Mp3Directory.TAG_MODE, "Mono");
                    break;
            }

            // Copyright boolean
            int copyright = (header & 0x00000008) >> 3;
            switch (copyright)
            {
                case 0:
                    directory.Set(Mp3Directory.TAG_COPYRIGHT, "False");
                    break;
                case 1:
                    directory.Set(Mp3Directory.TAG_COPYRIGHT, "True");
                    break;
            }

            int emphasis = header & 0x00000003;
            switch (emphasis)
            {
                case 0:
                    directory.Set(Mp3Directory.TAG_EMPHASIS, "none");
                    break;
                case 1:
                    directory.Set(Mp3Directory.TAG_EMPHASIS, "50/15ms");
                    break;
                case 3:
                    directory.Set(Mp3Directory.TAG_EMPHASIS, "CCITT j.17");
                    break;
            }

            int frameSize = SetBitrate(bitrate, layer, id) * 1000 * 144 / frequency;
            directory.Set(Mp3Directory.TAG_FRAME_SIZE, frameSize + " bytes");

            return directory;
        }

        private static int SetBitrate(int bitrate, int layer, int id)
        {
            ushort[,] bitrateMapping = new ushort[14, 6]
            {
                { 32, 32, 32, 32, 32, 8 },
                { 64, 48, 40, 64, 48, 16 },
                { 96, 56, 48, 96, 56, 24 },
                { 128, 64, 56, 128, 64, 32 },
                { 160, 80, 64, 160, 80, 64 },
                { 192, 96, 80, 192, 96, 80 },
                { 224, 112, 96, 224, 112, 56 },
                { 256, 128, 112, 256, 128, 64 },
                { 288, 160, 128, 28, 160, 128 },
                { 320, 192, 160, 320, 192, 160 },
                { 352, 224, 192, 352, 224, 112 },
                { 384, 256, 224, 384, 256, 128 },
                { 416, 320, 256, 416, 320, 256 },
                { 448, 384, 320, 448, 384, 320 }
            };

            int xPos = 0;
            int yPos = bitrate - 1;

            if (id == 2)
            {
                // MPEG-2
                switch (layer)
                {
                    case 1:
                        xPos = 5;
                        break;
                    case 2:
                        xPos = 4;
                        break;
                    case 3:
                        xPos = 3;
                        break;
                }
            }
            else if (id == 1)
            {
                // MPEG-1
                switch (layer)
                {
                    case 1:
                        xPos = 2;
                        break;
                    case 2:
                        xPos = 1;
                        break;
                    case 3:
                        xPos = 0;
                        break;
                }
            }

            return bitrateMapping[yPos, xPos];
        }
    }
}
