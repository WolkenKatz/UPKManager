﻿using System;


namespace UpkManager.Dds.Constants {

  [Flags]
  public enum SurfaceFlags {

    Texture = 0x00001000, // DDSCAPS_TEXTURE
    MipMap  = 0x00400008, // DDSCAPS_COMPLEX | DDSCAPS_MIPMAP
    CubeMap = 0x00000008  // DDSCAPS_COMPLEX

  }

}
