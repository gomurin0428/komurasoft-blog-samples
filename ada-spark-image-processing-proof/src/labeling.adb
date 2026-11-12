package body Labeling with SPARK_Mode is

   procedure Find
     (Parent : Parent_Table;
      L      : Label_Id;
      Root   : out Label_Id)
   is
   begin
      Root := L;
      while Parent (Root) /= Root loop
         Root := Parent (Root);
      end loop;
   end Find;

   procedure Label_Components
     (Source  : Image;
      Labels  : in out Label_Image;
      Parent  : in out Parent_Table;
      Count   : out Natural;
      Success : out Boolean)
   is
      Next      : Natural range 1 .. Max_Labels + 1 := 1;
      Up, Left  : Label;
      Root_Up   : Label_Id;
      Root_Left : Label_Id;
      Root      : Label_Id;
   begin
      Count   := 0;
      Success := False;

      --  1回目: 上と左の画素のラベルを見て、仮のラベルを付ける
      for Y in Source'Range (1) loop
         for X in Source'Range (2) loop
            if Source (Y, X) = 0 then
               Labels (Y, X) := 0;
            else
               Up   := (if Y > Source'First (1) then Labels (Y - 1, X) else 0);
               Left := (if X > Source'First (2) then Labels (Y, X - 1) else 0);

               if Up = 0 and then Left = 0 then
                  if Next > Max_Labels then
                     return;  --  ラベルが足りない
                  end if;
                  Parent (Next) := Next;
                  Labels (Y, X) := Next;
                  Next := Next + 1;
               elsif Left = 0 then
                  Labels (Y, X) := Up;
               elsif Up = 0 then
                  Labels (Y, X) := Left;
               else
                  Find (Parent, Up, Root_Up);
                  Find (Parent, Left, Root_Left);
                  if Root_Up < Root_Left then
                     Parent (Root_Left) := Root_Up;
                  elsif Root_Left < Root_Up then
                     Parent (Root_Up) := Root_Left;
                  end if;
                  Labels (Y, X) := Label'Min (Root_Up, Root_Left);
               end if;
            end if;
         end loop;
      end loop;

      --  2回目: 仮のラベルを、つながった成分の代表のラベルに置き換える
      for Y in Source'Range (1) loop
         for X in Source'Range (2) loop
            if Labels (Y, X) /= 0 then
               Find (Parent, Labels (Y, X), Root);
               Labels (Y, X) := Root;
            end if;
         end loop;
      end loop;

      for L in 1 .. Next - 1 loop
         pragma Loop_Invariant (Count < L);
         if Parent (L) = L then
            Count := Count + 1;
         end if;
      end loop;

      Success := True;
   end Label_Components;

end Labeling;
