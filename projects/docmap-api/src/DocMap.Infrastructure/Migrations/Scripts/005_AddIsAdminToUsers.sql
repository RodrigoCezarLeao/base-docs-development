ALTER TABLE users ADD COLUMN IF NOT EXISTS is_admin BOOLEAN NOT NULL DEFAULT FALSE;

-- Promote an admin manually:  UPDATE users SET is_admin = TRUE WHERE email = 'you@example.com';
