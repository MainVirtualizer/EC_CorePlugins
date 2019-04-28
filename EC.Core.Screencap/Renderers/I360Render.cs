﻿using System;
using System.Linq;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

namespace EC.Core.Screencap.Renderers
{
    /// <summary>
    /// Code ported from https://github.com/yasirkula/Unity360ScreenshotCapture/
    /// </summary>
    public static class I360Render
    {
        private static Material equirectangularConverter;

        public static void Init()
        {
            if (equirectangularConverter != null) return;

            var abd = Properties.Resources.EquirectangularConverter;
            var ab = AssetBundle.LoadFromMemory(abd);
            equirectangularConverter = new Material(ab.LoadAsset<Shader>("assets/shaders/equirectangularconverter.shader"));
            ab.Unload(false);
        }

        public static byte[] Capture(int width = 1024, bool encodeAsJPEG = true, Camera renderCam = null)
        {
            if (renderCam == null)
            {
                renderCam = Camera.main;
                if (renderCam == null) throw new Exception("No camera detected");
            }

            var disableTypes = new[]
            {
                typeof(VignetteAndChromaticAberration),
                typeof(DepthOfField)
            };
            var disabled = renderCam.gameObject.GetComponents<Behaviour>().Where(x => x.enabled && disableTypes.Contains(x.GetType())).ToArray();
            foreach (var comp in disabled) comp.enabled = false;

            var camTarget = renderCam.targetTexture;

            var cubemapSize = Mathf.Min(Mathf.NextPowerOfTwo(width), 16384);
            RenderTexture cubemap = null, equirectangularTexture = null;
            Texture2D output = null;
            try
            {
                cubemap = RenderTexture.GetTemporary(cubemapSize, cubemapSize, 0);
                cubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;

                equirectangularTexture = RenderTexture.GetTemporary(cubemapSize, cubemapSize / 2, 0);
                equirectangularTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;

                if (!renderCam.RenderToCubemap(cubemap))
                    throw new Exception("Rendering to cubemap is not supported on device/platform");

                Graphics.Blit(cubemap, equirectangularTexture, equirectangularConverter);
                output = RtToT2D(equirectangularTexture);

                return encodeAsJPEG ? InsertXMPIntoTexture2D_JPEG(output) : InsertXMPIntoTexture2D_PNG(output);
            }
            finally
            {
                renderCam.targetTexture = camTarget;

                if (cubemap != null)
                    RenderTexture.ReleaseTemporary(cubemap);

                if (equirectangularTexture != null)
                    RenderTexture.ReleaseTemporary(equirectangularTexture);

                if (output != null)
                    UnityEngine.Object.DestroyImmediate(output);

                foreach (var comp in disabled) comp.enabled = true;
            }
        }

        private static Texture2D RtToT2D(RenderTexture equirectangularTexture)
        {
            var temp = RenderTexture.active;
            RenderTexture.active = equirectangularTexture;
            var output = new Texture2D(equirectangularTexture.width, equirectangularTexture.height, TextureFormat.RGB24, false);
            output.ReadPixels(new Rect(0, 0, equirectangularTexture.width, equirectangularTexture.height), 0, 0);
            RenderTexture.active = temp;
            return output;
        }

        #region XMP Injection

        private const string XMP_NAMESPACE_JPEG = "http://ns.adobe.com/xap/1.0/";
        private const string XMP_CONTENT_TO_FORMAT_JPEG = "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\" x:xmptk=\"Adobe XMP Core 5.1.0-jc003\"> <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"> <rdf:Description rdf:about=\"\" xmlns:GPano=\"http://ns.google.com/photos/1.0/panorama/\" GPano:UsePanoramaViewer=\"True\" GPano:CaptureSoftware=\"Unity3D\" GPano:StitchingSoftware=\"Unity3D\" GPano:ProjectionType=\"equirectangular\" GPano:PoseHeadingDegrees=\"180.0\" GPano:InitialViewHeadingDegrees=\"0.0\" GPano:InitialViewPitchDegrees=\"0.0\" GPano:InitialViewRollDegrees=\"0.0\" GPano:InitialHorizontalFOVDegrees=\"{0}\" GPano:CroppedAreaLeftPixels=\"0\" GPano:CroppedAreaTopPixels=\"0\" GPano:CroppedAreaImageWidthPixels=\"{1}\" GPano:CroppedAreaImageHeightPixels=\"{2}\" GPano:FullPanoWidthPixels=\"{1}\" GPano:FullPanoHeightPixels=\"{2}\"/></rdf:RDF></x:xmpmeta>";
        private const string XMP_CONTENT_TO_FORMAT_PNG = "XML:com.adobe.xmp\0\0\0\0\0<?xpacket begin=\"ï»¿\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?><x:xmpmeta xmlns:x=\"adobe:ns:meta/\" x:xmptk=\"Adobe XMP Core 5.1.0-jc003\"> <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"> <rdf:Description rdf:about=\"\" xmlns:GPano=\"http://ns.google.com/photos/1.0/panorama/\" xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:xmpMM=\"http://ns.adobe.com/xap/1.0/mm/\" xmlns:stEvt=\"http://ns.adobe.com/xap/1.0/sType/ResourceEvent#\" xmlns:tiff=\"http://ns.adobe.com/tiff/1.0/\" xmlns:exif=\"http://ns.adobe.com/exif/1.0/\"> <GPano:UsePanoramaViewer>True</GPano:UsePanoramaViewer> <GPano:CaptureSoftware>Unity3D</GPano:CaptureSoftware> <GPano:StitchingSoftware>Unity3D</GPano:StitchingSoftware> <GPano:ProjectionType>equirectangular</GPano:ProjectionType> <GPano:PoseHeadingDegrees>180.0</GPano:PoseHeadingDegrees> <GPano:InitialViewHeadingDegrees>0.0</GPano:InitialViewHeadingDegrees> <GPano:InitialViewPitchDegrees>0.0</GPano:InitialViewPitchDegrees> <GPano:InitialViewRollDegrees>0.0</GPano:InitialViewRollDegrees> <GPano:InitialHorizontalFOVDegrees>{0}</GPano:InitialHorizontalFOVDegrees> <GPano:CroppedAreaLeftPixels>0</GPano:CroppedAreaLeftPixels> <GPano:CroppedAreaTopPixels>0</GPano:CroppedAreaTopPixels> <GPano:CroppedAreaImageWidthPixels>{1}</GPano:CroppedAreaImageWidthPixels> <GPano:CroppedAreaImageHeightPixels>{2}</GPano:CroppedAreaImageHeightPixels> <GPano:FullPanoWidthPixels>{1}</GPano:FullPanoWidthPixels> <GPano:FullPanoHeightPixels>{2}</GPano:FullPanoHeightPixels> <tiff:Orientation>1</tiff:Orientation> <exif:PixelXDimension>{1}</exif:PixelXDimension> <exif:PixelYDimension>{2}</exif:PixelYDimension> </rdf:Description></rdf:RDF></x:xmpmeta><?xpacket end=\"w\"?>";
        private static uint[] CRC_TABLE_PNG;

        public static byte[] InsertXMPIntoTexture2D_JPEG(Texture2D image)
        {
            return DoTheHardWork_JPEG(image.EncodeToJPG(100), image.width, image.height);
        }

        public static byte[] InsertXMPIntoTexture2D_PNG(Texture2D image)
        {
            return DoTheHardWork_PNG(image.EncodeToPNG(), image.width, image.height);
        }

        #region JPEG Encoding

        private static byte[] DoTheHardWork_JPEG(byte[] fileBytes, int imageWidth, int imageHeight)
        {
            int xmpIndex = 0, xmpContentSize = 0;
            while (!SearchChunkForXMP_JPEG(fileBytes, ref xmpIndex, ref xmpContentSize))
            {
                if (xmpIndex == -1)
                    break;
            }

            int copyBytesUntil, copyBytesFrom;
            if (xmpIndex == -1)
                copyBytesUntil = copyBytesFrom = FindIndexToInsertXMPCode_JPEG(fileBytes);
            else
            {
                copyBytesUntil = xmpIndex;
                copyBytesFrom = xmpIndex + 2 + xmpContentSize;
            }

            var xmpContent = string.Concat(XMP_NAMESPACE_JPEG, "\0", string.Format(XMP_CONTENT_TO_FORMAT_JPEG, 75f.ToString("F1"), imageWidth, imageHeight));
            var xmpLength = xmpContent.Length + 2;
            xmpContent = string.Concat((char)0xFF, (char)0xE1, (char)(xmpLength / 256), (char)(xmpLength % 256), xmpContent);

            var result = new byte[copyBytesUntil + xmpContent.Length + (fileBytes.Length - copyBytesFrom)];

            Array.Copy(fileBytes, 0, result, 0, copyBytesUntil);

            for (var i = 0; i < xmpContent.Length; i++)
                result[copyBytesUntil + i] = (byte)xmpContent[i];

            Array.Copy(fileBytes, copyBytesFrom, result, copyBytesUntil + xmpContent.Length, fileBytes.Length - copyBytesFrom);

            return result;
        }

        private static bool CheckBytesForXMPNamespace_JPEG(byte[] bytes, int startIndex)
        {
            for (var i = 0; i < XMP_NAMESPACE_JPEG.Length; i++)
            {
                if (bytes[startIndex + i] != XMP_NAMESPACE_JPEG[i])
                    return false;
            }

            return true;
        }

        private static bool SearchChunkForXMP_JPEG(byte[] bytes, ref int startIndex, ref int chunkSize)
        {
            if (startIndex + 4 < bytes.Length)
            {
                //Debug.Log( startIndex + " " + System.Convert.ToByte( bytes[startIndex] ).ToString( "x2" ) + " " + System.Convert.ToByte( bytes[startIndex+1] ).ToString( "x2" ) + " " +
                //           System.Convert.ToByte( bytes[startIndex+2] ).ToString( "x2" ) + " " + System.Convert.ToByte( bytes[startIndex+3] ).ToString( "x2" ) );

                if (bytes[startIndex] == 0xFF)
                {
                    var secondByte = bytes[startIndex + 1];
                    if (secondByte == 0xDA)
                    {
                        startIndex = -1;
                        return false;
                    }
                    if (secondByte == 0x01 || secondByte >= 0xD0 && secondByte <= 0xD9)
                    {
                        startIndex += 2;
                        return false;
                    }
                    chunkSize = bytes[startIndex + 2] * 256 + bytes[startIndex + 3];

                    if (secondByte == 0xE1 && chunkSize >= 31 && CheckBytesForXMPNamespace_JPEG(bytes, startIndex + 4))
                        return true;

                    startIndex = startIndex + 2 + chunkSize;
                }
            }

            return false;
        }

        private static int FindIndexToInsertXMPCode_JPEG(byte[] bytes)
        {
            var chunkSize = bytes[4] * 256 + bytes[5];
            return chunkSize + 4;
        }

        #endregion

        #region PNG Encoding

        private static byte[] DoTheHardWork_PNG(byte[] fileBytes, int imageWidth, int imageHeight)
        {
            var xmpContent = "iTXt" + string.Format(XMP_CONTENT_TO_FORMAT_PNG, 75f.ToString("F1"), imageWidth, imageHeight);
            var copyBytesUntil = 33;
            var xmpLength = xmpContent.Length - 4; // minus iTXt
            var xmpCRC = CalculateCRC_PNG(xmpContent);
            xmpContent = string.Concat(
                (char)(xmpLength >> 24), (char)(xmpLength >> 16), (char)(xmpLength >> 8), (char)xmpLength,
                xmpContent, xmpCRC);

            var result = new byte[fileBytes.Length + xmpContent.Length];

            Array.Copy(fileBytes, 0, result, 0, copyBytesUntil);

            for (var i = 0; i < xmpContent.Length; i++)
                result[copyBytesUntil + i] = (byte)xmpContent[i];

            Array.Copy(fileBytes, copyBytesUntil, result, copyBytesUntil + xmpContent.Length, fileBytes.Length - copyBytesUntil);

            return result;
        }

        // Source: https://github.com/damieng/DamienGKit/blob/master/CSharp/DamienG.Library/Security/Cryptography/Crc32.cs
        private static string CalculateCRC_PNG(string xmpContent)
        {
            if (CRC_TABLE_PNG == null)
                CalculateCRCTable_PNG();

            var crc = ~UpdateCRC_PNG(xmpContent);
            var crcBytes = CalculateCRCBytes_PNG(crc);

            return string.Concat((char)crcBytes[0], (char)crcBytes[1], (char)crcBytes[2], (char)crcBytes[3]);
        }

        private static uint UpdateCRC_PNG(string xmpContent)
        {
            var c = 0xFFFFFFFF;
            for (var i = 0; i < xmpContent.Length; i++)
                c = (c >> 8) ^ CRC_TABLE_PNG[xmpContent[i] ^ (c & 0xFF)];

            return c;
        }

        private static void CalculateCRCTable_PNG()
        {
            CRC_TABLE_PNG = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                var c = i;
                for (var j = 0; j < 8; j++)
                {
                    if ((c & 1) == 1)
                        c = (c >> 1) ^ 0xEDB88320;
                    else
                        c = c >> 1;
                }

                CRC_TABLE_PNG[i] = c;
            }
        }

        private static byte[] CalculateCRCBytes_PNG(uint crc)
        {
            var result = BitConverter.GetBytes(crc);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return result;
        }

        #endregion

        #endregion
    }
}