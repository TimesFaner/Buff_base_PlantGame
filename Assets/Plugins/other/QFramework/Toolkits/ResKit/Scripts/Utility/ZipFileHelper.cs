/****************************************************************************
 * Copyright (c) 2017 snowcold
 * Copyright (c) 2017 ~ 2020.10 liangxie
 *
 * https://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 ****************************************************************************/

using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;

namespace QFramework
{
    public class ZipFileHelper : IZipFileHelper
    {
        private readonly List<string> mSearchDirList = new();
        private readonly ZipFile mZipFile = null;
        private string mStreamingAssetsPath;

        public ZipFileHelper()
        {
            mSearchDirList.Add(AssetBundlePathHelper.PersistentDataPath4Res);
            mStreamingAssetsPath = AssetBundlePathHelper.StreamingAssetsPath;
#if (UNITY_ANDROID) && !UNITY_EDITOR
			if (mZipFile == null)
			{
			mZipFile = new ZipFile(File.Open(UnityEngine.Application.dataPath, FileMode.Open, FileAccess.Read));
			}
#endif
        }

        public Stream OpenReadStream(string absFilePath)
        {
            if (string.IsNullOrEmpty(absFilePath)) return null;

#if UNITY_ANDROID && !UNITY_EDITOR
			//Android 包内
			if (absFilePath.Contains(".apk/"))
			{
			return OpenStreamInZip(absFilePath);
			}
#endif
            var fileInfo = new FileInfo(absFilePath);

            if (!fileInfo.Exists) return null;

            return fileInfo.OpenRead();
        }

        public void GetFileInInner(string fileName, List<string> outResult)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
			//Android 包内
			GetFileInZip(mZipFile, fileName, outResult);
			return;
#endif
            AssetBundlePathHelper.GetFileInFolder(AssetBundlePathHelper.StreamingAssetsPath, fileName, outResult);
        }

        public ZipFile GetZipFile()
        {
            return mZipFile;
        }


        ~ZipFileHelper()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
			if (mZipFile != null)
			{
			mZipFile.Close();
			mZipFile = null;
			}
#endif
        }

        public void InitStreamingAssetPath()
        {
            mStreamingAssetsPath = AssetBundlePathHelper.StreamingAssetsPath;
        }

        //在包内查找是否有改资源
        private bool FindResInAppInternal(string fileRelativePath)
        {
#if UNITY_IPHONE && !UNITY_EDITOR
			string absoluteFilePath = FindFilePathInternal(fileRelativePath);
			return !string.IsNullOrEmpty(absoluteFilePath);
#elif UNITY_ANDROID && !UNITY_EDITOR
			int entryIndex = mZipFile.FindEntry(string.Format("assets/{0}", fileRelativePath), false);
			return entryIndex != -1;
#else
            var absoluteFilePath = string.Format("{0}{1}", mStreamingAssetsPath, fileRelativePath);
            return File.Exists(absoluteFilePath);
#endif
        }

        private void AddSearchPath(string dir)
        {
            mSearchDirList.Add(dir);
        }

        public bool FileExists(string fileRelativePath)
        {
#if UNITY_IPHONE && !UNITY_EDITOR
			string absoluteFilePath = FindFilePath(fileRelativePath);
			return (!string.IsNullOrEmpty(absoluteFilePath) && File.Exists(absoluteFilePath));
#elif UNITY_ANDROID && !UNITY_EDITOR
			string absoluteFilePath = FindFilePathInExteral(fileRelativePath);
			//先到外存去找
			if (!string.IsNullOrEmpty(absoluteFilePath))
			{
			return File.Exists(absoluteFilePath);
			}
			else
			{
			if (mZipFile == null)
			{
			return false;
			}

			return mZipFile.FindEntry(string.Format("assets/{0}", fileRelativePath), true) >= 0;
			}
#else
            var filePathStandalone = string.Format("{0}{1}", mStreamingAssetsPath, fileRelativePath);
            return !string.IsNullOrEmpty(filePathStandalone) && File.Exists(filePathStandalone);
#endif
        }

        public byte[] ReadSync(string fileRelativePath)
        {
            var absoluteFilePath = FindFilePathInExteral(fileRelativePath);
            if (!string.IsNullOrEmpty(absoluteFilePath)) return ReadSyncExtenal(fileRelativePath);

            return ReadSyncInternal(fileRelativePath);
        }

        public byte[] ReadSyncByAbsoluteFilePath(string absoluteFilePath)
        {
            if (File.Exists(absoluteFilePath))
            {
                var fileInfo = new FileInfo(absoluteFilePath);
                return ReadFile(fileInfo);
            }

            return null;
        }

        private byte[] ReadSyncExtenal(string fileRelativePath)
        {
            var absoluteFilePath = FindFilePathInExteral(fileRelativePath);

            if (!string.IsNullOrEmpty(absoluteFilePath))
            {
                var fileInfo = new FileInfo(absoluteFilePath);
                return ReadFile(fileInfo);
            }

            return null;
        }

        private byte[] ReadSyncInternal(string fileRelativePath)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
			return ReadDataInAndriodApk(fileRelativePath);
#else
            var absoluteFilePath = FindFilePathInternal(fileRelativePath);

            if (!string.IsNullOrEmpty(absoluteFilePath))
            {
                var fileInfo = new FileInfo(absoluteFilePath);
                return ReadFile(fileInfo);
            }
#endif

            return null;
        }


        private byte[] ReadFile(FileInfo fileInfo)
        {
            using (var fileStream = fileInfo.OpenRead())
            {
                var byteData = new byte[fileStream.Length];
                fileStream.Read(byteData, 0, byteData.Length);
                return byteData;
            }
        }

        private string FindFilePathInExteral(string file)
        {
            string filePath;
            for (var i = 0; i < mSearchDirList.Count; ++i)
            {
                filePath = string.Format("{0}/{1}", mSearchDirList[i], file);
                if (File.Exists(filePath)) return filePath;
            }

            return string.Empty;
        }

        private string FindFilePath(string file)
        {
            // 先到搜索列表里找
            var filePath = FindFilePathInExteral(file);
            if (!string.IsNullOrEmpty(filePath)) return filePath;

            // 在包内找
            filePath = FindFilePathInternal(file);
            if (!string.IsNullOrEmpty(filePath)) return filePath;

            return null;
        }

        private string FindFilePathInternal(string file)
        {
            var filePath = string.Format("{0}{1}", mStreamingAssetsPath, file);

            if (File.Exists(filePath)) return filePath;

            return null;
        }


        private Stream OpenStreamInZip(string absPath)
        {
            var tag = "!/assets/";
            var androidFolder = absPath.Substring(0, absPath.IndexOf(tag));

            var startIndex = androidFolder.Length + tag.Length;
            var relativePath = absPath.Substring(startIndex, absPath.Length - startIndex);

            var zipEntry = mZipFile.GetEntry(string.Format("assets/{0}", relativePath));

            if (zipEntry != null)
                return mZipFile.GetInputStream(zipEntry);
            Debug.LogError($"Can't Find File {absPath}");

            return null;
        }

        public void GetFileInZip(ZipFile zipFile, string fileName, List<string> outResult)
        {
            var totalCount = 0;

            foreach (var entry in zipFile)
            {
                ++totalCount;
                var e = entry as ZipEntry;
                if (e != null)
                    if (e.IsFile)
                        if (e.Name.EndsWith(fileName))
                            outResult.Add(zipFile.Name + "/!/" + e.Name);
            }
        }


        private byte[] ReadDataInAndriodApk(string fileRelativePath)
        {
            byte[] byteData = null;
            //Log.I("Read From In App...");
            if (mZipFile == null)
            {
                Debug.LogError("can't open apk");
                return null;
            }

            //Log.I("Begin Open Zip...");
            var zipEntry = mZipFile.GetEntry(string.Format("assets/{0}", fileRelativePath));
            //Log.I("End Open Zip...");
            if (zipEntry != null)
            {
                //Log.I("Begin Read Zip...");
                var stream = mZipFile.GetInputStream(zipEntry);
                byteData = new byte[zipEntry.Size];
                //Log.I("Read Zip Length:" + byteData.Length);
                stream.Read(byteData, 0, byteData.Length);
                stream.Close();
            }
            else
            {
                Debug.LogError(string.Format("Can't Find File {0}", fileRelativePath));
            }

            return byteData;
        }
    }
}