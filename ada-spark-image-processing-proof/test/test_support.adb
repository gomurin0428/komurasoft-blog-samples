package body Test_Support is

   use type Interfaces.Unsigned_32;

   function New_Image (Width, Height : Positive) return Image_Access is
   begin
      return new Image'(0 .. Height - 1 => (0 .. Width - 1 => 0));
   end New_Image;

   function New_Labels (Width, Height : Positive) return Label_Image_Access is
   begin
      return new Label_Image'(0 .. Height - 1 => (0 .. Width - 1 => 0));
   end New_Labels;

   procedure Fill_Random (Img : in out Image; Seed : Interfaces.Unsigned_32) is
      State : Interfaces.Unsigned_32 := Seed;
   begin
      for Y in Img'Range (1) loop
         for X in Img'Range (2) loop
            State := State * 1_103_515_245 + 12_345;
            Img (Y, X) := Pixel (Interfaces.Shift_Right (State, 16) and 16#FF#);
         end loop;
      end loop;
   end Fill_Random;

end Test_Support;
