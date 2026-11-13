using Catlab

# 頂点に名前を持つグラフ（業務の状態）と、その「開いた」版
@present SchNamedGraph <: SchGraph begin
  Name::AttrType
  vname::Attr(V, Name)
end
@acset_type NamedGraph(SchNamedGraph, index=[:src, :tgt])
# 頂点（V）で外とつなぐ開いたグラフの型。中身は構造化コスパン
const OpenNamedGraphOb, OpenNamedGraph = OpenACSetTypes(NamedGraph, :V)

# 状態の名前の列から、1本道のグラフを作る
function flow(names)
  g = NamedGraph{String}()
  add_parts!(g, :V, length(names), vname=names)
  add_parts!(g, :E, length(names) - 1, src=1:length(names)-1, tgt=2:length(names))
  g
end

# 受注部門の流れ: 受付 → 在庫確認 → 出荷待ち（1本目の脚は「受付」、2本目の脚は「出荷待ち」）
order = flow(["受付", "在庫確認", "出荷待ち"])
order_open = OpenNamedGraph{String}(order, FinFunction([1], 3), FinFunction([3], 3))

# 物流部門の流れ: 出荷待ち → 梱包 → 発送
ship = flow(["出荷待ち", "梱包", "発送"])
ship_open = OpenNamedGraph{String}(ship, FinFunction([1], 3), FinFunction([3], 3))

# 2つを「出荷待ち」でつなぐ合成のしかたを、配線図で書く
seq = @relation (a, c) where (a, b, c) begin
  order(a, b)
  ship(b, c)
end
whole = apex(oapply(seq, Dict(:order => order_open, :ship => ship_open)))

println("状態: ", whole[:vname])
for e in parts(whole, :E)
  println(whole[whole[e, :src], :vname], " → ", whole[whole[e, :tgt], :vname])
end

# つなぐ点の名前が合わない部品（「出荷準備」から始まる）は、つなげない
prep = flow(["出荷準備", "梱包", "発送"])
prep_open = OpenNamedGraph{String}(prep, FinFunction([1], 3), FinFunction([3], 3))
try
  oapply(seq, Dict(:order => order_open, :ship => prep_open))
catch e
  println("つなげない: ", first(split(sprint(showerror, e), ':')))
end
