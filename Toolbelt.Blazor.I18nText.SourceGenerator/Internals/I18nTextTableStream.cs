using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Toolbelt.Blazor.I18nText.SourceGenerator.Internals
{
    internal class I18nTextTableStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotImplementedException();

        private long _InternalPosition = 0;

        public override long Position { get => this._InternalPosition; set => throw new NotImplementedException(); }

        private IEnumerator<KeyValuePair<string, I18nTextTable>> LangEnumerator { get; }

        private IEnumerator<KeyValuePair<string, string>> TextEnumerator { get; set; }

        private byte[] Buffer { get; set; }

        private int BufferSeekPointer { get; set; }

        public I18nTextTableStream(I18nTextType i18nText)
        {
            this.LangEnumerator = i18nText.Langs.OrderBy(l => l.Key).GetEnumerator();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.Buffer != null) return this.ReadFromBuffer(buffer, offset, count);

            for (; ; )
            {
                if (this.TextEnumerator == null)
                {
                    if (!this.LangEnumerator.MoveNext()) return 0;

                    var currentLang = this.LangEnumerator.Current;
                    this.TextEnumerator = currentLang.Value.OrderBy(l => l.Key).GetEnumerator();

                    this.Buffer = Encoding.UTF8.GetBytes(currentLang.Key);
                    return this.ReadFromBuffer(buffer, offset, count);
                }

                if (!this.TextEnumerator.MoveNext())
                {
                    this.TextEnumerator = null;
                    continue;
                }

                var currentText = this.TextEnumerator.Current;
                this.Buffer = Encoding.UTF8.GetBytes(currentText.Key + currentText.Value);
                return this.ReadFromBuffer(buffer, offset, count);
            }
        }

        private int ReadFromBuffer(byte[] buffer, int offset, int count)
        {
            var cbRead = Math.Min(this.Buffer.Length - this.BufferSeekPointer, count);
            Array.Copy(this.Buffer, this.BufferSeekPointer, buffer, offset, cbRead);
            this.BufferSeekPointer += cbRead;
            if (this.Buffer.Length <= this.BufferSeekPointer)
            {
                this.Buffer = null;
                this.BufferSeekPointer = 0;
            }
            this._InternalPosition += cbRead;
            return cbRead;
        }

        public override void Flush() { /* NOP */}
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
    }
}

