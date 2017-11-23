class Inengine < Formula
  desc "A cross-platform .NET program that allows commands to be queued, scheduled, and run directly."
  homepage "http://inengine.net"
  url "https://github.com/InEngine-NET/InEngine.NET/archive/3.3.0.tar.gz"
  sha256 "2b64c91104df74451bcb1f55df5c7c4d822d9bcd56f9fa0fb48ca8f43aa70f1d"
  depends_on "mono"
  depends_on "nuget"

  def install
    system "nuget", "restore", "./src"
    system "xbuild", "/p:Configuration=Release", "./src/InEngine.Net.sln"
  end

  test do
    system "false"
  end
end

