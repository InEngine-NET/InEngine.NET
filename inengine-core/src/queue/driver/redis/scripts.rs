use std::sync::LazyLock;

pub(super) static MOVE_IF_PRESENT: LazyLock<redis::Script> = LazyLock::new(|| {
    redis::Script::new(
        r"
if redis.call('LREM', KEYS[1], 1, ARGV[1]) == 1 then
  redis.call('LPUSH', KEYS[2], ARGV[2])
  return 1
end
return 0
",
    )
});

pub(super) static BULK_MOVE: LazyLock<redis::Script> = LazyLock::new(|| {
    redis::Script::new(
        r"
local moved = 0
while moved < tonumber(ARGV[1]) do
  if not redis.call('LMOVE', KEYS[1], KEYS[2], ARGV[2], ARGV[3]) then
    break
  end
  moved = moved + 1
end
return moved
",
    )
});
