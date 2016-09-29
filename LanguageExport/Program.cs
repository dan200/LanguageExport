using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Dan200.Tools.LanguageExport
{
    class Program
    {
        public static void PrintUsage()
        {
            Console.WriteLine( "Usage:" );
            Console.WriteLine( "LanguageExport.exe <google spreadsheet url> <output folder> [language code]+" );
        }

        public static string GetExportURL( string inputURL )
        {
            var url = new Uri( inputURL );
            var exportUrl = new UriBuilder( url );
            if( Path.GetFileName( exportUrl.Path ).Length < 10 ) // Remove "edit", "export" or whatever from the URL
            {
                exportUrl.Path = Path.GetDirectoryName( url.AbsolutePath );
            }
            exportUrl.Path = exportUrl.Path + "/export";

            var gidPos = exportUrl.Query.IndexOf( "gid=" );
            if( gidPos >= 0 )
            {
                var gidEndPos = exportUrl.Query.IndexOf( "&", gidPos );
                var gid = ( gidEndPos >= 0 ) ?
                    exportUrl.Query.Substring( gidPos + 4, gidEndPos - (gidEndPos + 4) ) :
                    exportUrl.Query.Substring( gidPos + 4 );
                exportUrl.Query = "format=csv&gid=" + gid;
            }
            else
            {
                exportUrl.Query = "format=csv";
            }
            return exportUrl.ToString();
        }

        private static string[] ParseCSVRow( TextReader reader )
        {
            string line;
            if( (line = reader.ReadLine()) == null )
            {
                return null;
            }

            var results = new List<string>();
            var parts = line.Split( ',' );
            int i = 0;
            while( i < parts.Length )
            {
                string data = parts[ i ];
                if( data.StartsWith( "\"", StringComparison.InvariantCulture ) )
                {
                    while( !data.EndsWith( "\"", StringComparison.InvariantCulture ) )
                    {
                        if( i < parts.Length - 1 )
                        {
                            i++;
                            data += ",";
                        }
                        else
                        {
                            line = reader.ReadLine();
                            parts = line.Split( ',' );
                            i = 0;
                            data += "\\n";
                        }
                        data += parts[ 0 ];
                    }
                    data = data.Substring( 1, data.Length - 2 );
                }
                results.Add( data );
                ++i;
            }
            return results.ToArray();
        }

        public static void Main( string[] args )
        {
            // Parse the program arguments
            if( args.Length < 2 )
            {
                PrintUsage();
                return;
            }
            var exportURL = GetExportURL( args[ 0 ] );
            var outputPath = args[ 1 ];

            ISet<string> filterLanguages = null;
            if( args.Length >= 3 )
            {
                filterLanguages = new HashSet<string>();
                for( int i = 2; i < args.Length; ++i )
                {
                    filterLanguages.Add( args[ i ] );
                }
            }

            // Start the export
            try
            {
                Console.WriteLine( "Connecting to {0}", exportURL );
                var csvRequest = HttpWebRequest.Create( exportURL );
                using( var csvResponse = csvRequest.GetResponse() )
                {
                    using( var responseStream = csvResponse.GetResponseStream() )
                    {
                        var reader = new StreamReader( responseStream, Encoding.UTF8 );
                        try
                        {
                            // Get the names of all the languages from the first row
                            string[] languages = ParseCSVRow( reader );
                            TextWriter[] writers = new TextWriter[ languages.Length ];
                            for( int i = 1; i < languages.Length; ++i )
                            {
                                string languageCode = languages[ i ];
                                if( languageCode.Length > 0 &&
                                    (filterLanguages == null || filterLanguages.Contains( languageCode ) ) )
                                {
                                    Console.WriteLine( "Found language {0}", languageCode );
                                    writers[ i ] = new StreamWriter( Path.Combine( outputPath, string.Format( "{0}.lang", languageCode ) ) );
                                    writers[ i ].WriteLine( "// {0} translations", languageCode );
                                }
                            }

                            // Get the actual translations from the rest of the rows
                            try
                            {
                                string[] translations;
                                while( (translations = ParseCSVRow(reader)) != null )
                                {
                                    if( translations.Length > 0 && translations[ 0 ].Length > 0 )
                                    {
                                        string symbol = translations[ 0 ];
                                        for( int i = 1; i < languages.Length; ++i )
                                        {
                                            if( i < translations.Length && translations[ i ].Length > 0 )
                                            {
                                                if( writers[ i ] != null )
                                                {
                                                    writers[ i ].WriteLine( "{0}={1}", symbol, translations[ i ] );
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                for( int i = 1; i < languages.Length; ++i )
                                {
                                    if( writers[ i ] != null )
                                    {
                                        writers[ i ].Close();
                                    }
                                }
                            }

                            // Finish
                            Console.WriteLine( "All languages exported" );
                        }
                        finally
                        {
                            reader.Close();
                        }
                    }
                }
            }
            catch( Exception e )
            {
                Console.WriteLine( "{0}: {0}", e.GetType().Name, e.Message );
            }
        }
    }
}
