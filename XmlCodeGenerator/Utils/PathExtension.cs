using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

namespace BclExtension
{
    /// <summary>
    /// 경로와 관련된 유틸리티 함수 모음
    /// </summary>
    public static class PathExtension
    {
        /// <summary>
        /// 일반 경로를 UNC 형식의 경로로 반환
        /// </summary>
        /// <param name="ipAddress">IP 또는 컴퓨터 이름</param>
        /// <param name="path">절대 경로</param>
        /// <returns>UNC 포맷 경로</returns>
        public static string ConvertToUNCPath(string ipAddress, string path)
        {
            if (Path.IsPathRooted(path) == false)
            {
                if (path != null && path.Length == 1)
                {
                    return string.Format(CultureInfo.CurrentCulture, @"\\{0}\{1}$", ipAddress, path);
                }

                return string.Empty;
            }

            string fileName = Path.GetFileName(path);
            string driveLetter = Path.GetPathRoot(path);
            string folder = Path.GetDirectoryName(path);

            if (folder != null && folder.Length >= 3)
            {
                folder = folder.Substring(3);
            }

            string uncPath = string.Format(CultureInfo.CurrentCulture, @"\\{0}\{1}$\{2}",
              ipAddress, driveLetter.Substring(0, 1), folder);

            return Path.Combine(uncPath, fileName).TrimEnd(Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// 명령행 인자를 가지고 있는 경로에서 파일명을 반환
        /// </summary>
        /// <param name="path">경로</param>
        /// <returns>파일명</returns>
        public static string GetFileNameFromIncludingInvalidChars(string path)
        {
            char[] invalidChars = Path.GetInvalidPathChars();
            foreach (char ch in invalidChars)
            {
                path = path.Replace(ch, ' ');
            }

            string converted = Path.GetFileName(path);
            return converted.Split(' ')[0];
        }

        /// <summary>
        /// 경로명에서 파일명을 바꿔준다.
        /// </summary>
        /// <param name="path">파일 경로</param>
        /// <param name="newFileName">새로운 파일명</param>
        /// <returns>파일명이 교체된 경로</returns>
        public static string ChangeFileName(string path, string newFileName)
        {
            string parent = Path.GetDirectoryName(path);
            return Path.Combine(parent, newFileName);
        }

        /// <summary>
        /// 경로에서 drive 문자를 반환
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static char GetDriveLetter(string path)
        {
            return path[0];
        }

        /// <summary>
        /// URL 을 연결해서 반환
        /// </summary>
        /// <param name="baseUri">URL 폴더</param>
        /// <param name="fileName">파일명</param>
        /// <returns></returns>
        public static Uri UrlCombine(string baseUri, string fileName)
        {
            if (baseUri.EndsWith("/") == false)
            {
                baseUri += "/";
            }

            Uri uri = new Uri(baseUri);
            Uri sourceUri = new Uri(uri, fileName);

            return sourceUri;
        }

        /// <summary>
        /// Shell 의 open 명령 실행
        /// </summary>
        /// <param name="path">폴더 경로</param>
        public static void OpenVerb(string path)
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo("iexplore.exe");
            startInfo.Arguments = path;
            System.Diagnostics.Process.Start(startInfo);
        }

        /// <summary>
        /// 파일명에 따라 부모 폴더를 검색해 가면서 파일이 있는 경우에만 경로를 반환
        /// </summary>
        /// <param name="path">검색 시작 경로 및 파일명</param>
        /// <returns>검색된 파일명</returns>
        public static string SearchInParents(string path)
        {
            if (File.Exists(path) == true)
            {
                return path;
            }

            string folder = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);


            while (true)
            {
                DirectoryInfo dir = Directory.GetParent(folder);
                if (dir == null)
                {
                    break;
                }

                string filePath = Path.Combine(dir.FullName, fileName);
                if (File.Exists(filePath) == true)
                {
                    return filePath;
                }

                folder = dir.FullName;
            }

            return string.Empty;
        }

    }
}

