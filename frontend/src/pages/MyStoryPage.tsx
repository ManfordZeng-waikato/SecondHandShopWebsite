import { Box, Container, Divider, Stack, Typography } from '@mui/material';

const paragraphs = [
  'Hi, my name is Pat Jackson. For many years I have been a teacher in primary schools around the Waikato.',
  'During that time, I developed a hobby for buying quality used furniture, equipment, clothing, decor, and anything else that was good quality, mainly for my home or school classrooms. I have always preferred to buy used instead of new.',
  "My hobby turned into a passion and now, 25 years on, I have retired from teaching and have set up a small family business, along with my husband and son, working from my shed, Pat's Shed.",
  'Working from home also gives me the opportunity to take care of my lovely parents who live across the driveway from us. Be sure to say hi if you see them.',
  "Please drop by for a visit, I would love to show you around Pat's Shed.",
  'Looking forward to seeing you soon!',
];

export function MyStoryPage() {
  return (
    <Container maxWidth="md" sx={{ py: { xs: 3, md: 5 } }}>
      {/* Page heading */}
      <Box sx={{ mb: 4 }}>
        <Typography
          component="span"
          sx={{
            display: 'inline-block',
            mb: 1.5,
            letterSpacing: '0.14em',
            textTransform: 'uppercase',
            fontSize: '0.72rem',
            fontWeight: 600,
            color: 'text.secondary',
          }}
        >
          Hamilton, New Zealand
        </Typography>
        <Typography
          variant="h3"
          component="h1"
          sx={{
            fontSize: { xs: '2rem', md: '2.8rem' },
            lineHeight: 1.1,
          }}
        >
          My Story
        </Typography>
      </Box>

      <Divider sx={{ mb: 4 }} />

      {/* Story content */}
      <Box
        sx={{
          bgcolor: '#f0ebe4',
          border: '1px solid',
          borderColor: 'divider',
          borderRadius: 3,
          p: { xs: 3, sm: 4, md: 5 },
          position: 'relative',
          overflow: 'hidden',
        }}
      >
        {/* Watermark */}
        <Box
          aria-hidden
          component="img"
          src="/Title.svg"
          sx={{
            position: 'absolute',
            bottom: -20,
            right: -20,
            width: '55%',
            height: 'auto',
            opacity: 0.05,
            pointerEvents: 'none',
            userSelect: 'none',
          }}
        />

        {/* Opening quote mark */}
        <Box
          aria-hidden
          sx={{
            fontFamily: "'Cormorant Garamond', Georgia, serif",
            fontSize: { xs: '4rem', md: '6rem' },
            lineHeight: 0.65,
            color: 'primary.main',
            opacity: 0.2,
            mb: 2,
            userSelect: 'none',
          }}
        >
          &ldquo;
        </Box>

        <Stack spacing={2.5} sx={{ position: 'relative' }}>
          {paragraphs.map((text, i) => (
            <Typography
              key={i}
              variant="h6"
              sx={{
                color: '#3f3f3f',
                fontWeight: 400,
                lineHeight: 1.8,
                fontStyle: i === paragraphs.length - 1 ? 'italic' : 'normal',
              }}
            >
              {text}
            </Typography>
          ))}
        </Stack>

        {/* Signature */}
        <Box
          sx={{
            mt: 4,
            pt: 3,
            borderTop: '1px solid',
            borderColor: 'divider',
            display: 'flex',
            alignItems: 'center',
            gap: 1.5,
          }}
        >
          <Box
            aria-hidden
            sx={{ width: 36, height: '2px', bgcolor: 'primary.main', flexShrink: 0 }}
          />
          <Typography
            sx={{
              fontFamily: "'Cormorant Garamond', Georgia, serif",
              fontSize: '1.1rem',
              fontStyle: 'italic',
              color: 'text.secondary',
              fontWeight: 500,
            }}
          >
            Pat Jackson, Pat's Shed
          </Typography>
        </Box>
      </Box>
    </Container>
  );
}
