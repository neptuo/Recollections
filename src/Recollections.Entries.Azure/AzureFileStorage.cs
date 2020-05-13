using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class AzureFileStorage : IFileStorage
    {
        void Test()
        {
            // Parse the connection string for the storage account.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("$CONNECTIONSTRING$");

            // Create a CloudFileClient object for credentialed access to Azure Files.
            CloudFileClient fileClient = storageAccount.CreateCloudFileClient();

            // Get a reference to the file share we created previously.
            CloudFileShare share = fileClient.GetShareReference("entries");

            // Ensure that the share exists.
            if (share.Exists())
            {
                // Get a reference to the root directory for the share.
                CloudFileDirectory rootDir = share.GetRootDirectoryReference();

                // Get a reference to the directory we created previously.
                CloudFileDirectory sampleDir = rootDir.GetDirectoryReference("CustomLogs");

                // Ensure that the directory exists.
                if (sampleDir.Exists())
                {
                    // Get a reference to the file we created previously.
                    CloudFile sourceFile = sampleDir.GetFileReference("Log1.txt");

                    // Ensure that the source file exists.
                    if (sourceFile.Exists())
                    {
                        // Get a reference to the destination file.
                        CloudFile destFile = sampleDir.GetFileReference("Log1Copy.txt");

                        // Start the copy operation.
                        destFile.StartCopy(sourceFile);

                        // Write the contents of the destination file to the console window.
                        Console.WriteLine(destFile.DownloadText());
                    }
                }
            }
        }

        public Task DeleteAsync(Entry entry, Image image, ImageType type)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> FindAsync(Entry entry, Image image, ImageType type)
        {
            throw new NotImplementedException();
        }

        public Task SaveAsync(Entry entry, Image image, Stream content, ImageType type)
        {
            throw new NotImplementedException();
        }
    }
}
