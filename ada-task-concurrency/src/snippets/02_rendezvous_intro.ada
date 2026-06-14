-- 02_rendezvous_intro.ada
-- Rendezvous: the caller blocks until the task accepts the entry.
-- Data can be passed in both directions via entry parameters.

with Ada.Text_IO; use Ada.Text_IO;

procedure Rendezvous_Demo is

   task Worker is
      entry Compute (X, Y : Integer; Result : out Integer);
   end Worker;

   task body Worker is
      A, B   : Integer;
      Output : Integer;
   begin
      accept Compute (X, Y : Integer; Result : out Integer) do
         A := X;
         B := Y;
         Output := A * A + B * B;
         Result := Output;
      end Compute;
   end Worker;

   Answer : Integer;
begin
   Put_Line ("Main: calling Worker.Compute...");
   Worker.Compute (3, 4, Answer);
   Put_Line ("Main: result = " & Integer'Image (Answer));
end Rendezvous_Demo;