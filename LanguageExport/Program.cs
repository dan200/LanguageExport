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
            Console.WriteLine( "LanguageExport <google spreadsheet url> <output folder>" );
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
            exportUrl.Query = "format=csv";
            return exportUrl.ToString();
        }

        private static string[] ParseCSVRow( string line )
        {
            char separator = ',';
            List<string> parsed = new List<string>();
            string[] temp = line.Split( separator );
            int counter = 0;
            while( counter < temp.Length )
            {
                string data = temp[ counter ];
                if( data.StartsWith( "\"" ) )
                {
                    while( !data.EndsWith( "\"" ) )
                    {
                        data += separator.ToString();
                        data += temp[ ++counter ];
                    }
                    data = data.Substring( 1, data.Length - 2 );
                }
                parsed.Add( data );
                counter++;
            }
            return parsed.ToArray();
        }

        public static void Main( string[] args )
        {
            // Parse the program arguments
            if( args.Length != 2 )
            {
                PrintUsage();
                return;
            }
            var exportURL = GetExportURL( args[ 0 ] );
            var outputPath = args[ 1 ];

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
                            string firstLine = reader.ReadLine();
                            string[] languages = ParseCSVRow( firstLine );
                            TextWriter[] writers = new TextWriter[ languages.Length ];
                            for( int i = 1; i < languages.Length; ++i )
                            {
                                string languageCode = languages[ i ];
                                if( languageCode.Length > 0 )
                                {
                                    Console.WriteLine( "Found language {0}", languageCode );
                                    writers[ i ] = new StreamWriter( Path.Combine( outputPath, string.Format( "{0}.lang", languageCode ) ) );
                                    writers[ i ].WriteLine( "// {0} Translations", languageCode );
                                }
                            }

                            // Get the actual translations from the rest of the rows
                            try
                            {
                                string line;
                                while( (line = reader.ReadLine()) != null )
                                {
                                    string[] translations = ParseCSVRow( line );
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
