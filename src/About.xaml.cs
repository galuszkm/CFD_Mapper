using System.Windows;

namespace CFD_Mapper
{
    /// <summary>
    /// Logika interakcji dla klasy Window.xaml
    /// </summary>
    public partial class About : Window
    {

        public About()
        {
            InitializeComponent();
            License_Box.AppendText(License());
            Source_Box.AppendText(Source());
            Packages_Box.AppendText(Packages());
        }

        private string License()
        {
            string license =
                "# BSD License\n"+
                "Copyright (c) 2020 Michal Galuszka\n" +
                "All rights reserved.\n\n" +
                "Redistribution and use in source and binary forms, with or without modification, are permitted provided " +
                "that the following conditions are met:\n" +
                " * Redistributions of source code must retain the above copyright notice, this list of conditions " +
                "and the following disclaimer.\n" +
                " * Redistributions in binary form must reproduce the above copyright notice, this list of conditions " +
                "and the following disclaimer in the documentation and/or other materials provided with the distribution.\n" +
                " * Neither name of Michal Galuszka nor the names of any contributors may be used to endorse " +
                "or promote products derived from this software without specific prior written permission.\n\n" +
                "THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ''AS IS'' AND ANY EXPRESS OR IMPLIED " +
                "WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR " +
                "A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHORS OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, " +
                "INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, " +
                "PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) " +
                "HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING " +
                "NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY " +
                "OF SUCH DAMAGE.";

            return license;
        }

        private string Packages()
        {
            string packages =
                "ActiViz .NET x64 (v5.8.0):     https://www.kitware.eu/activiz/ " + "\n" +
                "Costura.Fody (v4.1.0):     https://github.com/Fody/Costura";

            return packages;
        }

        private string Source()
        {
            string source =
                "Download:     https://github.com/galuszkm/CFD_Mapper.git \n" +
                "Please report any bugs or comments at:      michal.galuszka1@gmail.com";

            return source;
        }

    }
}