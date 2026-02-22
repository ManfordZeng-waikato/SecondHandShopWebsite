import { Paper, Stack, Typography } from '@mui/material';

export function MyStoryPage() {
  return (
    <Stack spacing={2}>
      <Typography variant="h3">My Story</Typography>
      <Paper sx={{ p: 3 }}>
        <Stack spacing={2}>
          <Typography variant="h6" sx={{ color: '#3f3f3f' }} fontWeight={400}>
            Hi, my name is Pat Jackson. For many years I have been a teacher in primary schools
            around the Waikato.
          </Typography>
          <Typography variant="h6" sx={{ color: '#3f3f3f' }} fontWeight={400}>
            During that time, I developed a hobby for buying quality used furniture, equipment,
            clothing, decor, and anything else that was good quality, mainly for my home or school
            classrooms. I have always preferred to buy used instead of new.
          </Typography>
          <Typography variant="h6" sx={{ color: '#3f3f3f' }} fontWeight={400}>
            My hobby turned into a passion and now, 25 years on, I have retired from teaching and
            have set up a small family business, along with my husband and son, working from my
            shed, Pat's Shed.
          </Typography>
          <Typography variant="h6" sx={{ color: '#3f3f3f' }} fontWeight={400}>
            Working from home also gives me the opportunity to take care of my lovely parents who
            live across the driveway from us. Be sure to say hi if you see them.
          </Typography>
          <Typography variant="h6" sx={{ color: '#3f3f3f' }} fontWeight={400}>
            Please drop by for a visit, I would love to show you around Pat's Shed.
          </Typography>
          <Typography variant="h6" sx={{ color: '#3f3f3f' }} fontWeight={400}>
            Looking forward to seeing you soon!
          </Typography>
        </Stack>
      </Paper>
    </Stack>
  );
}
