import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Waktu Solat Malaysia",
  description: "Malaysian prayer times from e-solat.gov.my",
  icons: {
    icon: "/favicon.ico",
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className="antialiased">
        {children}
      </body>
    </html>
  );
}
