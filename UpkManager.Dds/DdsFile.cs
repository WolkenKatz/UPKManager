﻿using System;
using System.IO;

using UpkManager.Dds.Constants;


namespace UpkManager.Dds {

  public class DdsFile {

    #region Private Fields

    private const uint ddsSignature = 0x20534444;

    private DdsHeader header;

    #endregion Private Fields

    #region Public Properties

    public int Width => (int)header.Width;

    public int Height => (int)header.Height;

    public byte[] PixelData { get; private set; }

    #endregion Public Properties

    #region Public Methods

    public void Load(Stream input) {
      BinaryReader reader = new BinaryReader(input);
      //
      // Read the DDS tag. If it's not right, then bail..
      //
      uint signature = reader.ReadUInt32();

      if (signature != ddsSignature) throw new FormatException("File does not appear to be a DDS image");

      header = new DdsHeader();
      //
      // Read everything in.. for now assume it worked like a charm..
      //
      header.Read(reader);

      Load(header, input);
    }

    public void Load(DdsHeader ddsHeader, Stream input) {
      header = ddsHeader;

      if ((header.PixelFormat.Flags & (int)PixelFormatFlags.FourCC) != 0) {
        int squishFlags;

        switch(header.PixelFormat.FourCC) {
          case FourCCFormat.Dxt1: {
            squishFlags = (int)SquishFlags.Dxt1;

            break;
          }
          case FourCCFormat.Dxt3: {
            squishFlags = (int)SquishFlags.Dxt3;

            break;
          }

          case FourCCFormat.Dxt5: {
            squishFlags = (int)SquishFlags.Dxt5;

            break;
          }
          default: {
            throw new FormatException("File is not a supported DDS format");
          }
        }
        //
        // Compute size of compressed block area
        //
        int blockCount = (Width + 3) / 4 * ((Height + 3) / 4);
        int blockSize  = (squishFlags & (int)SquishFlags.Dxt1) != 0 ? 8 : 16;
        //
        // Allocate room for compressed blocks, and read data into it.
        //
        byte[] compressedBlocks = new byte[blockCount * blockSize];

        input.Read(compressedBlocks, 0, compressedBlocks.GetLength(0));

        // Now decompress..
        PixelData = DdsSquish.DecompressImage(Width, Height, compressedBlocks, squishFlags, null);
      }
      else {
        //
        // We can only deal with the non-DXT formats we know about..  this is a bit of a mess..
        // Sorry..
        //
        FileFormat fileFormat = FileFormat.Unknown;

        if ((header.PixelFormat.Flags == (int)PixelFormatFlags.RGBA) && (header.PixelFormat.RgbBitCount == 32) &&
            (header.PixelFormat.ABitMask == 0x00ff0000) && (header.PixelFormat.GBitMask == 0x0000ff00) &&
            (header.PixelFormat.BBitMask == 0x000000ff) && (header.PixelFormat.ABitMask == 0xff000000)) fileFormat = FileFormat.A8R8G8B8;
        else if ((header.PixelFormat.Flags == (int)PixelFormatFlags.RGB) && (header.PixelFormat.RgbBitCount == 32) &&
                 (header.PixelFormat.RBitMask == 0x00ff0000) && (header.PixelFormat.GBitMask == 0x0000ff00) &&
                 (header.PixelFormat.BBitMask == 0x000000ff) && (header.PixelFormat.ABitMask == 0x00000000)) fileFormat = FileFormat.X8R8G8B8;
        else if ((header.PixelFormat.Flags == (int)PixelFormatFlags.RGBA) && (header.PixelFormat.RgbBitCount == 32) &&
                 (header.PixelFormat.RBitMask == 0x000000ff) && (header.PixelFormat.GBitMask == 0x0000ff00) &&
                 (header.PixelFormat.BBitMask == 0x00ff0000) && (header.PixelFormat.ABitMask == 0xff000000)) fileFormat = FileFormat.A8B8G8R8;
        else if ((header.PixelFormat.Flags == (int)PixelFormatFlags.RGB) && (header.PixelFormat.RgbBitCount == 32) &&
                 (header.PixelFormat.RBitMask == 0x000000ff) && (header.PixelFormat.GBitMask == 0x0000ff00) &&
                 (header.PixelFormat.BBitMask == 0x00ff0000) && (header.PixelFormat.ABitMask == 0x00000000)) fileFormat = FileFormat.X8B8G8R8;
        else if ((header.PixelFormat.Flags == (int)PixelFormatFlags.RGBA) && (header.PixelFormat.RgbBitCount == 16) &&
                 (header.PixelFormat.RBitMask == 0x00007c00) && (header.PixelFormat.GBitMask == 0x000003e0) &&
                 (header.PixelFormat.BBitMask == 0x0000001f) && (header.PixelFormat.ABitMask == 0x00008000)) fileFormat = FileFormat.A1R5G5B5;
        else if ((header.PixelFormat.Flags == (int)PixelFormatFlags.RGBA) && (header.PixelFormat.RgbBitCount == 16) &&
                 (header.PixelFormat.RBitMask == 0x00000f00) && (header.PixelFormat.GBitMask == 0x000000f0) &&
                 (header.PixelFormat.BBitMask == 0x0000000f) && (header.PixelFormat.ABitMask == 0x0000f000)) fileFormat = FileFormat.A4R4G4B4;
        else if ((header.PixelFormat.Flags == (int)PixelFormatFlags.RGB) && (header.PixelFormat.RgbBitCount == 24) &&
                 (header.PixelFormat.RBitMask == 0x00ff0000) && (header.PixelFormat.GBitMask == 0x0000ff00) &&
                 (header.PixelFormat.BBitMask == 0x000000ff) && (header.PixelFormat.ABitMask == 0x00000000)) fileFormat = FileFormat.R8G8B8;
        else if ((header.PixelFormat.Flags == (int)PixelFormatFlags.RGB) && (header.PixelFormat.RgbBitCount == 16) &&
                 (header.PixelFormat.RBitMask == 0x0000f800) && (header.PixelFormat.GBitMask == 0x000007e0) &&
                 (header.PixelFormat.BBitMask == 0x0000001f) && (header.PixelFormat.ABitMask == 0x00000000)) fileFormat = FileFormat.R5G6B5;
        //
        // If fileFormat is still invalid, then it's an unsupported format.
        //
        if (fileFormat == FileFormat.Unknown) throw new FormatException("File is not a supported DDS format");
        //
        // Size of a source pixel, in bytes
        //
        int srcPixelSize = ((int)header.PixelFormat.RgbBitCount / 8);
        //
        // We need the pitch for a row, so we can allocate enough memory for the load.
        //
        int rowPitch;

        if ((header.HeaderFlags & (int)HeaderFlags.Pitch) != 0) {
          //
          // Pitch specified.. so we can use directly
          //
          rowPitch = (int)header.PitchOrLinearSize;
        }
        else if ((header.HeaderFlags & (int)HeaderFlags.LinearSize) != 0) {
          //
          // Linear size specified.. compute row pitch. Of course, this should never happen
          // as linear size is *supposed* to be for compressed textures. But Microsoft don't
          // always play by the rules when it comes to DDS output.
          //
          rowPitch = (int)header.PitchOrLinearSize / (int)header.Height;
        }
        else {
          //
          // Another case of Microsoft not obeying their standard is the 'Convert to..' shell extension
          // that ships in the DirectX SDK. Seems to always leave flags empty..so no indication of pitch
          // or linear size. And - to cap it all off - they leave pitchOrLinearSize as *zero*. Zero??? If
          // we get this bizarre set of inputs, we just go 'screw it' and compute row pitch ourselves.
          //
          rowPitch = (int)header.Width * srcPixelSize;
        }
        //
        // Ok.. now, we need to allocate room for the bytes to read in from.. it's rowPitch bytes * height
        //
        byte[] readPixelData = new byte[rowPitch * header.Height];

        input.Read(readPixelData, 0, readPixelData.GetLength(0));
        //
        // We now need space for the real pixel data.. that's width * height * 4..
        //
        PixelData = new byte[header.Width * header.Height * 4];
        //
        // And now we have the arduous task of filling that up with stuff..
        //
        for(int destY = 0; destY < (int)header.Height; destY++) {
          for(int destX = 0; destX < (int)header.Width; destX++) {
            //
            // Compute source pixel offset
            //
            int srcPixelOffset = destY * rowPitch + destX * srcPixelSize;
            //
            // Read our pixel
            //
            uint pixelColour = 0;
            uint pixelRed    = 0;
            uint pixelGreen  = 0;
            uint pixelBlue   = 0;
            uint pixelAlpha  = 0;
            //
            // Build our pixel colour as a DWORD
            //
            for(int loop = 0; loop < srcPixelSize; loop++) pixelColour |= (uint)(readPixelData[srcPixelOffset + loop] << (8 * loop));

            switch(fileFormat) {
              case FileFormat.A8R8G8B8: {
                pixelAlpha = (pixelColour >> 24) & 0xff;
                pixelRed   = (pixelColour >> 16) & 0xff;
                pixelGreen = (pixelColour >> 8)  & 0xff;
                pixelBlue  = (pixelColour >> 0)  & 0xff;

                break;
              }
              case FileFormat.X8R8G8B8: {
                pixelAlpha = 0xff;

                pixelRed   = (pixelColour >> 16) & 0xff;
                pixelGreen = (pixelColour >> 8)  & 0xff;
                pixelBlue  = (pixelColour >> 0)  & 0xff;

                break;
              }
              case FileFormat.A8B8G8R8: {
                pixelAlpha = (pixelColour >> 24) & 0xff;
                pixelRed   = (pixelColour >> 0)  & 0xff;
                pixelGreen = (pixelColour >> 8)  & 0xff;
                pixelBlue  = (pixelColour >> 16) & 0xff;

                break;
              }
              case FileFormat.X8B8G8R8: {
                pixelAlpha = 0xff;

                pixelRed   = (pixelColour >> 0)  & 0xff;
                pixelGreen = (pixelColour >> 8)  & 0xff;
                pixelBlue  = (pixelColour >> 16) & 0xff;

                break;
              }
              case FileFormat.A1R5G5B5: {
                pixelAlpha = (pixelColour >> 15) & 0xff;
                pixelRed   = (pixelColour >> 10) & 0x1f;
                pixelGreen = (pixelColour >> 5)  & 0x1f;
                pixelBlue  = (pixelColour >> 0)  & 0x1f;

                pixelRed   = (pixelRed   << 3) | (pixelRed   >> 2);
                pixelGreen = (pixelGreen << 3) | (pixelGreen >> 2);
                pixelBlue  = (pixelBlue  << 3) | (pixelBlue  >> 2);

                break;
              }
              case FileFormat.A4R4G4B4: {
                pixelAlpha = (pixelColour >> 12) & 0xff;
                pixelRed   = (pixelColour >> 8)  & 0x0f;
                pixelGreen = (pixelColour >> 4)  & 0x0f;
                pixelBlue  = (pixelColour >> 0)  & 0x0f;

                pixelAlpha = (pixelAlpha << 4) | (pixelAlpha >> 0);
                pixelRed   = (pixelRed   << 4) | (pixelRed   >> 0);
                pixelGreen = (pixelGreen << 4) | (pixelGreen >> 0);
                pixelBlue  = (pixelBlue  << 4) | (pixelBlue  >> 0);

                break;
              }
              case FileFormat.R8G8B8: {
                pixelAlpha = 0xff;

                pixelRed   = (pixelColour >> 16) & 0xff;
                pixelGreen = (pixelColour >> 8)  & 0xff;
                pixelBlue  = (pixelColour >> 0)  & 0xff;

                break;
              }
              case FileFormat.R5G6B5: {
                pixelAlpha = 0xff;

                pixelRed   = (pixelColour >> 11) & 0x1f;
                pixelGreen = (pixelColour >> 5)  & 0x3f;
                pixelBlue  = (pixelColour >> 0)  & 0x1f;

                pixelRed   = (pixelRed   << 3) | (pixelRed   >> 2);
                pixelGreen = (pixelGreen << 2) | (pixelGreen >> 4);
                pixelBlue  = (pixelBlue  << 3) | (pixelBlue  >> 2);

                break;
              }
            }
            //
            // Write the colours away..
            //
            int destPixelOffset = destY * (int)header.Width * 4 + destX * 4;

            PixelData[destPixelOffset + 0] = (byte)pixelRed;
            PixelData[destPixelOffset + 1] = (byte)pixelGreen;
            PixelData[destPixelOffset + 2] = (byte)pixelBlue;
            PixelData[destPixelOffset + 3] = (byte)pixelAlpha;
          }
        }
      }
    }

    #endregion Public Methods

  }

}
