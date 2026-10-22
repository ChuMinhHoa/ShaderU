"""Tạo pattern low-poly seamless (tile 4 phía) với dải màu lấy từ ảnh tham chiếu.

Điểm được rải Poisson-disk trên mặt xuyến [0,1)^2, nhân bản 3x3 rồi Delaunay,
nên lưới tam giác tuần hoàn -> ảnh ra khớp mép tuyệt đối khi lặp.
Mỗi mặt tô gradient tuyến tính theo hướng ngẫu nhiên, màu tra từ ramp
(xanh đậm -> trắng) xây theo phân phối độ sáng của ảnh tham chiếu.

python lowpoly_seamless.py --ref ref.webp --out out.png --size 1024 --points 300
"""
import argparse

import numpy as np
from PIL import Image
from scipy.spatial import Delaunay


def poisson_torus(r, rng, k=30):
    """Bridson Poisson-disk trên mặt xuyến đơn vị (khoảng cách tính có wrap)."""
    n = int(np.ceil(np.sqrt(2) / r))
    cell = 1.0 / n
    grid = -np.ones((n, n), int)
    pts, active = [], []

    def add(p):
        grid[int(p[1] / cell) % n, int(p[0] / cell) % n] = len(pts)
        pts.append(p)
        active.append(len(pts) - 1)

    add(rng.random(2))
    reach = int(np.ceil(r / cell))
    while active:
        i = active[rng.integers(len(active))]
        for _ in range(k):
            ang = rng.random() * 2 * np.pi
            rad = r * (1 + rng.random())
            q = (pts[i] + rad * np.array([np.cos(ang), np.sin(ang)])) % 1.0
            gx, gy = int(q[0] / cell) % n, int(q[1] / cell) % n
            ok = True
            for dy in range(-reach, reach + 1):
                for dx in range(-reach, reach + 1):
                    j = grid[(gy + dy) % n, (gx + dx) % n]
                    if j >= 0:
                        d = np.abs(pts[j] - q)
                        d = np.minimum(d, 1 - d)
                        if d @ d < r * r:
                            ok = False
                            break
                if not ok:
                    break
            if ok:
                add(q)
                break
        else:
            active.remove(i)
    return np.array(pts)


def color_ramp(ref_path, steps=33, border=8):
    """Màu trung bình theo từng phân vị độ sáng (index 0 = đậm nhất, cuối = sáng nhất),
    kèm hướng + độ lệch chuẩn của sắc độ (xanh ngả cyan / ngả xám) ở cùng độ sáng."""
    a = np.asarray(Image.open(ref_path).convert("RGB"), dtype=np.float64)
    a = a[border:-border, border:-border].reshape(-1, 3)
    a = a[np.argsort(a.mean(1))]
    n, win = len(a), len(a) // (steps * 2)
    ramp = []
    for q in np.linspace(0, 1, steps):
        c = int(q * (n - 1))
        ramp.append(a[max(0, c - win):c + win + 1].mean(0))

    cs = np.vstack([np.zeros(3), np.cumsum(a, 0)])
    idx = np.arange(n)
    lo, hi = np.clip(idx - win, 0, n), np.clip(idx + win + 1, 0, n)
    res = a - (cs[hi] - cs[lo]) / (hi - lo)[:, None]
    w, v = np.linalg.eigh(np.cov(res.T))
    return np.array(ramp), v[:, -1], np.sqrt(w[-1])


def periodic_field(xy, rng, terms=6, max_freq=3):
    """Nhiễu tần số thấp tuần hoàn chu kỳ 1 (tần số nguyên), chuẩn hoá std ~1."""
    f = np.zeros(len(xy))
    for _ in range(terms):
        k = rng.integers(-max_freq, max_freq + 1, 2)
        if not k.any():
            k[0] = 1
        f += np.cos(2 * np.pi * (xy @ k) + rng.random() * 2 * np.pi)
    return f / np.sqrt(terms / 2)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--ref", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--size", type=int, default=1024)
    ap.add_argument("--points", type=int, default=300, help="số đỉnh xấp xỉ trong 1 tile")
    ap.add_argument("--drop", type=float, default=0.15, help="tỉ lệ đỉnh bỏ bớt để kích thước tam giác đa dạng")
    ap.add_argument("--jitter", type=float, default=0.35, help="dịch đỉnh, tính theo bán kính Poisson")
    ap.add_argument("--contrast", type=float, default=0.55, help="biên độ gradient trong mỗi mặt")
    ap.add_argument("--tint", type=float, default=1.0, help="độ lệch sắc độ cyan/xám, tính theo std của ảnh gốc")
    ap.add_argument("--ss", type=int, default=4, help="supersample mỗi chiều (khử răng cưa)")
    ap.add_argument("--seed", type=int, default=7)
    args = ap.parse_args()

    rng = np.random.default_rng(args.seed)
    ramp, tint_dir, tint_std = color_ramp(args.ref)

    # Mật độ Poisson-disk tối đa ~0.69/r^2, bù thêm phần bị drop.
    r = np.sqrt(0.69 / (args.points / (1 - args.drop)))
    pts = poisson_torus(r, rng)
    pts = pts[rng.random(len(pts)) >= args.drop]
    pts = (pts + rng.normal(0, args.jitter * r * 0.5, pts.shape)) % 1.0

    offsets = np.array([(dx, dy) for dy in (-1, 0, 1) for dx in (-1, 0, 1)], float)
    P = np.concatenate([pts + o for o in offsets])
    tri = Delaunay(P)
    verts = P[tri.simplices]  # (M, 3, 2)

    # Các bản sao của cùng một tam giác có cùng trọng tâm mod 1 -> cùng thuộc tính.
    frac = np.round((verts.mean(1) % 1.0) * 1e6).astype(np.int64) % 1000000
    keys, first, canon = np.unique(frac[:, 0] * 1000000 + frac[:, 1], return_index=True, return_inverse=True)
    n_canon = len(keys)
    centroid = verts.mean(1)[first] % 1.0
    theta = rng.random(n_canon) * 2 * np.pi
    u_mid = rng.random(n_canon)  # phân vị độ sáng -> khớp phân phối màu ảnh gốc
    amp = args.contrast * rng.uniform(0.3, 1.0, n_canon)
    # Sắc độ theo mảng lớn (tuần hoàn nên vẫn seamless) + lệch riêng từng mặt.
    tint = args.tint * tint_std * (periodic_field(centroid, rng) + 0.7 * rng.normal(0, 1, n_canon))

    d = np.stack([np.cos(theta), np.sin(theta)], 1)[canon]  # (M, 2)
    pv = np.einsum("mkj,mj->mk", verts, d)
    pmin, pspan = pv.min(1), np.maximum(pv.max(1) - pv.min(1), 1e-9)
    u_mid, amp, tint = u_mid[canon], amp[canon], tint[canon]

    S = args.size * args.ss
    xs = (np.arange(S) + 0.5) / S
    img = np.empty((args.size, args.size, 3), np.float32)
    rows = args.ss * 32
    ramp_x = np.linspace(0, 1, len(ramp))
    for y0 in range(0, S, rows):
        ys = xs[y0:y0 + rows]
        X, Y = np.meshgrid(xs, ys)
        q = np.stack([X.ravel(), Y.ravel()], 1)
        s = tri.find_simplex(q)
        assert (s >= 0).all(), "điểm mẫu nằm ngoài lưới"
        t = (np.einsum("nj,nj->n", q, d[s]) - pmin[s]) / pspan[s]
        u = np.clip(u_mid[s] + amp[s] * (t - 0.5), 0, 1)
        col = np.stack([np.interp(u, ramp_x, ramp[:, c]) for c in range(3)], 1)
        col += (tint[s] * (1 - 0.6 * u))[:, None] * tint_dir
        col = col.reshape(len(ys) // args.ss, args.ss, args.size, args.ss, 3).mean((1, 3))
        img[y0 // args.ss:(y0 + len(ys)) // args.ss] = col

    Image.fromarray(np.clip(img + 0.5, 0, 255).astype(np.uint8)).save(args.out)
    print(f"{args.out}: {len(pts)} verts, {n_canon} tris")


if __name__ == "__main__":
    main()
