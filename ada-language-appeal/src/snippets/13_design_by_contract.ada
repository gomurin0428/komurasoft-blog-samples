--  記事 13 章「契約による設計 ── Pre/Post条件を言語機能で書く」のコード断片。
--
--  Ada 2012では、サブプログラムに事前条件(Pre)と事後条件(Post)を
--  直接書ける。Preは「呼び出す側が守るべき約束」、Postは「実装側が
--  保証する約束」。'Old属性で呼び出し前の値を参照できる。
--  GNATでは -gnata オプションで実行時チェックとして有効化できる。
--
--  Pre違反  -> Assertion_Error(呼び出し側のバグ)
--  Post違反 -> Assertion_Error(実装側のバグ)

package Stacks is

   Capacity : constant := 100;

   type Stack is private;

   function Is_Full  (S : Stack) return Boolean;
   function Is_Empty (S : Stack) return Boolean;
   function Count    (S : Stack) return Natural;

   procedure Push (S : in out Stack; Item : Integer)
     with Pre  => not Is_Full (S),
          Post => Count (S) = Count (S)'Old + 1;

   procedure Pop (S : in out Stack; Item : out Integer)
     with Pre  => not Is_Empty (S),
          Post => Count (S) = Count (S)'Old - 1;

private
   type Integer_Array is array (1 .. Capacity) of Integer;

   type Stack is record
      Data : Integer_Array := (others => 0);
      Top  : Natural := 0;
   end record;

end Stacks;

package body Stacks is

   function Is_Full (S : Stack) return Boolean is
   begin
      return S.Top = Capacity;
   end Is_Full;

   function Is_Empty (S : Stack) return Boolean is
   begin
      return S.Top = 0;
   end Is_Empty;

   function Count (S : Stack) return Natural is
   begin
      return S.Top;
   end Count;

   procedure Push (S : in out Stack; Item : Integer) is
   begin
      S.Top := S.Top + 1;
      S.Data (S.Top) := Item;
   end Push;

   procedure Pop (S : in out Stack; Item : out Integer) is
   begin
      Item := S.Data (S.Top);
      S.Top := S.Top - 1;
   end Pop;

end Stacks;

with Ada.Text_IO;
with Stacks;

procedure Contracts_Demo is
   S    : Stacks.Stack;
   Item : Integer;
begin
   Stacks.Push (S, 10);
   Stacks.Push (S, 20);
   Stacks.Pop (S, Item);
   Ada.Text_IO.Put_Line ("popped:" & Integer'Image (Item));
   Ada.Text_IO.Put_Line ("count: " & Natural'Image (Stacks.Count (S)));
end Contracts_Demo;
