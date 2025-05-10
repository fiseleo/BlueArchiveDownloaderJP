using System.Text;


namespace BlueArchiveGUIDownloader
{
    public class RichTextBoxWriter : TextWriter
    {
        private readonly RichTextBox _output;
        public RichTextBoxWriter(RichTextBox output)
        {
            _output = output;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            if (_output.InvokeRequired)
                _output.Invoke(new Action(() => _output.AppendText(value.ToString())));
            else
                _output.AppendText(value.ToString());
        }

        public override void Write(string value)
        {
            if (_output.InvokeRequired)
                _output.Invoke(new Action(() => _output.AppendText(value)));
            else
                _output.AppendText(value);
        }

        public override void WriteLine(string value)
        {
            Write(value + Environment.NewLine);
        }
    }
}