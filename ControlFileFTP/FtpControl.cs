using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace ControlFileFTP
{
    public class FtpControl
    {        
        const string FORMAT_CREATE_PATH_FTP = "{0}/{1}";

        const string FORMAT_UPLOAD_FILE_FTP = "{0}/{1}/{2}";

        private FtpWebResponse _ftpWebResponse;

        private Stream _ftpStream;        

        public string UserName { get; set; }

        public string Password { get; set; }

        public string Host { get; set; }

        public FtpControl(string host, string user, string pass)
        {
            Host = host;
            UserName = user;
            Password = pass;
        }

        public bool CreateDirectory(string path)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (IsValid() == false)
            {
                return false;
            }

            try
            {
                //create directory on server
                CreateDirectoryOnServer(path);

                return true;
            }
            catch (WebException ex)
            {
                FtpWebResponse response = (FtpWebResponse)ex.Response;

                //directory already exists
                if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool DirectoryExists(string directory)
        {
            if (String.IsNullOrWhiteSpace(directory))
            {
                return false;
            }

            if (IsValid() == false)
            {
                return false;
            }

            try
            {
                FtpWebRequest requestDiretorio = FtpWebRequest.Create(Host) as FtpWebRequest;

                requestDiretorio.Credentials = new NetworkCredential(UserName, Password);
                requestDiretorio.Method = WebRequestMethods.Ftp.ListDirectory;
                StreamReader stream = new StreamReader(requestDiretorio.GetResponse().GetResponseStream());

                string values = stream.ReadLine();

                IList<string> listPath = new List<string>();

                while (values != null)
                {
                    listPath.Add(values);
                    values = stream.ReadLine();
                }

                foreach (var item in listPath)
                {
                    if (item.Equals(directory, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (WebException ex)
            {
                _ftpWebResponse = (FtpWebResponse)ex.Response;

                //verify if directory already exists on server
                if (_ftpWebResponse.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool RenameFile(string path, string nameFile, string renameFile)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (String.IsNullOrWhiteSpace(nameFile))
            {
                return false;
            }

            if (String.IsNullOrWhiteSpace(renameFile))
            {
                return false;
            }

            try
            {
                string host = String.Concat(this.Host, "/", path, "/", nameFile);

                FtpWebRequest requestDirectory = FtpWebRequest.Create(host) as FtpWebRequest;
                requestDirectory.Method = WebRequestMethods.Ftp.Rename;

                requestDirectory.Credentials = new NetworkCredential(this.UserName, this.Password);

                requestDirectory.UsePassive = true;
                requestDirectory.UseBinary = true;
                requestDirectory.KeepAlive = false;
                requestDirectory.RenameTo = renameFile;

                FtpWebResponse response = (FtpWebResponse)requestDirectory.GetResponse();
                Stream ftpStream = response.GetResponseStream();

                using (_ftpWebResponse = (FtpWebResponse)requestDirectory.GetResponse())
                {
                    _ftpStream = _ftpWebResponse.GetResponseStream();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Upload(string path, string fileName)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (String.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            if (IsValid() == false)
            {
                return false;
            }

            try
            {
                FtpWebRequest request = FtpWebRequest.Create(Host + "/" + path + "/" + Path.GetFileName(fileName)) as FtpWebRequest;

                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(this.UserName, this.Password);
                request.UsePassive = true;
                request.UseBinary = true;
                request.KeepAlive = false;

                //Load file
                FileStream stream = File.OpenRead(fileName);
                byte[] buffer = new byte[stream.Length];

                stream.Read(buffer, 0, buffer.Length);
                stream.Close();

                //Upload file
                Stream reqStream = request.GetRequestStream();
                reqStream.Write(buffer, 0, buffer.Length);
                reqStream.Close();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private void CreateDirectoryOnServer(string path)
        {
            //create directory on server
            FtpWebRequest requestDirectory = FtpWebRequest.Create(String.Format(FORMAT_CREATE_PATH_FTP, this.Host, path)) as FtpWebRequest;

            requestDirectory.Method = WebRequestMethods.Ftp.MakeDirectory;

            requestDirectory.Credentials = new NetworkCredential(this.UserName, this.Password);

            requestDirectory.UsePassive = true;
            requestDirectory.UseBinary = true;
            requestDirectory.KeepAlive = false;

            using (_ftpWebResponse = (FtpWebResponse)requestDirectory.GetResponse())
            {
                _ftpStream = _ftpWebResponse.GetResponseStream();
            }
        }

        private bool IsValid()
        {
            if (String.IsNullOrWhiteSpace(this.Host))
            {
                return false;
            }

            if (String.IsNullOrWhiteSpace(this.UserName))
            {
                return false;
            }

            if (String.IsNullOrWhiteSpace(this.Password))
            {
                return false;
            }

            return true;
        }
    }
}
