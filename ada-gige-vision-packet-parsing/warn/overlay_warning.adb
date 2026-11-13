--  番地で重ねる書き方に GNAT が出す警告を確かめる（記事では警告の文だけを引用する）。
with Gvsp;        use Gvsp;
with Gvsp_Layout; use Gvsp_Layout;

procedure Overlay_Warning is
   Buffer : aliased Byte_Array (0 .. 7) := (others => 0);
   H      : Standard_Header with Import, Address => Buffer'Address;
begin
   null;
end Overlay_Warning;
